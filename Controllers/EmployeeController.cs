using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Data;
using System.Data.SqlTypes;
using Microsoft.Extensions.Configuration;
using FYP.Models;

namespace FYP.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IConfiguration _configuration;

        public EmployeeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Schedule()
        {
            List<EmployeeSchedule> leaves = new List<EmployeeSchedule>();

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string query = "SELECT * FROM [leave] WHERE employee_id = @EmployeeId";

                connection.Open();

                leaves = connection.Query<EmployeeSchedule>(query, new { EmployeeId = 1 }).AsList();
            }

            return View(leaves);
        }

        public IActionResult ApplyLeave()
        {
            int nextLeaveId = GetNextLeaveId();

            ViewBag.NextLeaveId = nextLeaveId;

            return View();
        }

        [HttpPost]
        public IActionResult ApplyLeave(EmployeeSchedule leave)
        {
            int nextLeaveId = GetNextLeaveId();

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                EmployeeSchedule newLeave = new EmployeeSchedule
                {
                    EmployeeId = leave.EmployeeId,
                    LeaveId = nextLeaveId,
                    StartDate = leave.StartDate,
                    EndDate = leave.EndDate,
                    Reason = leave.Reason,
                    ProofProvided = leave.ProofProvided,
                    IsApproved = "pending"
                };

                string query = @"INSERT INTO [leave] (employee_id, leave_id, startDate, end_date, reason, proof_provided, is_approved)
                                 VALUES (@EmployeeId, @LeaveId, @StartDate, @EndDate, @Reason, @ProofProvided, @IsApproved)";

                if (connection.Execute(query, newLeave) == 1)
                {
                    TempData["Message"] = "Leave applied successfully";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Message"] = "Failed to apply leave";
                    TempData["MsgType"] = "danger";
                }
            }

            return RedirectToAction("Schedule", "Employee");
        }

        private int GetNextLeaveId()
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string query = "SELECT ISNULL(MAX(leave_id), 0) FROM [leave]";

                int nextLeaveId = connection.ExecuteScalar<int>(query) + 1;

                return nextLeaveId;
            }
        }

        
        

        

        
    }
}
