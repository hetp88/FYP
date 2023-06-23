using FYP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Data;
using System.Data.SqlTypes;
using Microsoft.Extensions.Configuration;

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

        public IActionResult ApplyLeave()
        {
            // Retrieve the next available leave_id
            int nextLeaveId = GetNextLeaveId();

            // Pass the nextLeaveId to the view
            ViewBag.NextLeaveId = nextLeaveId;

            return View();
        }

        [HttpPost]
        public IActionResult ApplyLeave(EmployeeSchedule leave)
        {
            // Retrieve the next available leave_id
            int nextLeaveId = GetNextLeaveId();

            if (leave.StartDate < SqlDateTime.MinValue.Value || leave.StartDate > SqlDateTime.MaxValue.Value ||
                leave.EndDate < SqlDateTime.MinValue.Value || leave.EndDate > SqlDateTime.MaxValue.Value)
            {
                TempData["Message"] = "Invalid date range. Please select dates within the allowed range.";
                TempData["MsgType"] = "danger";
                return RedirectToAction("ApplyLeave");
            }

            string insertLeaveQuery = @"INSERT INTO [leave] (leave_id, employee_id, startDate, end_date, reason, proof_provided, is_approved)
                                VALUES (@LeaveId, @EmployeeId, @StartDate, @EndDate, @Reason, @ProofProvided, 'pending')";

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                if (connection.Execute(insertLeaveQuery, new { LeaveId = nextLeaveId, leave }) == 1)
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

            return RedirectToAction("Index", "Home");
        }


        public IActionResult Schedule()
        {
            string selectLeaveQuery = "SELECT leave_id, startDate, end_date FROM [leave]";

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                var leaveData = connection.Query<EmployeeSchedule>(selectLeaveQuery).ToList();

                return View(leaveData);
            }
        }

        private int GetNextLeaveId()
        {
            string selectMaxLeaveIdQuery = "SELECT ISNULL(MAX(leave_id), 0) FROM [leave]";

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                int maxLeaveId = connection.ExecuteScalar<int>(selectMaxLeaveIdQuery);

                return maxLeaveId + 1;
            }
        }
    }
}
