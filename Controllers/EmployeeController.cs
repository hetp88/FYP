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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Security.Claims;

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
            List<EmployeeSchedule> schedule = new List<EmployeeSchedule>();

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string query = @"SELECT * FROM leave";

                schedule = connection.Query<EmployeeSchedule>(query).ToList();
            }

            return View(schedule);
        }
        private int GetNextLeaveId()
        {
            int leaveid = 0;
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();
                string query = $"SELECT MAX(leave_id) FROM leave";

                List<int> id = connection.Query<int>(query).AsList();
                foreach (int lid in id)
                {
                    leaveid = lid;
                }

                return leaveid;
            }
        }

        public IActionResult ApplyLeave()
        {
            //int nextLeaveId = GetNextLeaveId();

            //ViewBag.NextLeaveId = nextLeaveId;

            return View();
        }

        [HttpPost]
        public IActionResult ApplyLeave(EmployeeSchedule leave)
        {
            //int nextLeaveId = GetNextLeaveId();
            int leaveid = 0;
            int eid = 0;

            string currentemp = Environment.UserName;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string idquery = $"SELECT MAX(leave_id) FROM leave";
                string empquery = $"SELECT employee_id FROM employee WHERE name='{currentemp}'";

                List<int> id = connection.Query<int>(idquery).AsList();
                foreach (int lid in id)
                {
                    leaveid = lid;
                }

                List<int> emp = connection.Query<int>(empquery).AsList();
                foreach (int empid in emp)
                {
                    eid = empid;
                }

                EmployeeSchedule newLeave = new EmployeeSchedule
                {
                    EmployeeId = eid,
                    LeaveId = leaveid + 1,
                    StartDate = leave.StartDate,
                    EndDate = leave.EndDate,
                    Reason = leave.Reason,
                    ProofProvided = leave.ProofProvided,
                    IsApproved = "pending"
                };

                string insertQuery = @"INSERT INTO leave (employee_id, leave_id, startDate, end_date, reason, proof_provided, is_approved) VALUES (@EmployeeId, @LeaveId, @StartDate, @EndDate, @Reason, @ProofProvided, @IsApproved)";

                if (connection.Execute(insertQuery, newLeave) == 1)
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



        public IActionResult LeaveRequests()
        {
            List<EmployeeSchedule> LeaveRequests = new List<EmployeeSchedule>();

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string query = @"SELECT * FROM leave";

                LeaveRequests = connection.Query<EmployeeSchedule>(query).ToList();
            }

            return View(LeaveRequests);
        }


        [HttpGet]
        public IActionResult ReviewLeave(int leaveId)
        {
            EmployeeSchedule leave = null;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string query = "SELECT * FROM leave WHERE leave_id = @LeaveId";

                leave = connection.QueryFirstOrDefault<EmployeeSchedule>(query, new { LeaveId = leaveId });
            }

            if (leave == null)
            {
                TempData["Message"] = "Leave not found";
                TempData["MsgType"] = "danger";

                return RedirectToAction("LeaveRequests");
            }

            return View(leave);
        }

        [HttpPost]
        public IActionResult ReviewLeave(EmployeeSchedule leave)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string updateQuery = @"UPDATE leave SET is_approved = @IsApproved WHERE leave_id = @LeaveId";

                if (connection.Execute(updateQuery, leave) == 1)
                {
                    TempData["Message"] = "Leave reviewed successfully";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Message"] = "Failed to review leave";
                    TempData["MsgType"] = "danger";
                }
            }

            return RedirectToAction("LeaveRequests");
        }
        public IActionResult NewEmployee()
        {
            return View();
        }

    }
}
