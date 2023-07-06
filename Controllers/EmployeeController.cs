﻿using System;
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
using System.Collections;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

        public IActionResult EmployeeList()
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string query = @"SELECT e.employee_id AS EmployeeId, r.roles_type AS Role, e.name, e.email, e.phone_no, e.tickets AS no_tickets, e.closed_tickets AS closed_tickets
                FROM employee e
                INNER JOIN roles r ON r.roles_id = e.roles_id;";

                connection.Open();

                List<Employee> employees = connection.Query<Employee>(query).ToList();

                return View(employees);
            }
        }
        public IActionResult SearchEmployees(string employeeId, string role, string name, string email, string phoneNumber, string numTickets, string numclosed_tickets)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string query = @"SELECT e.employee_id AS EmployeeId, r.roles_type AS Role, e.name, e.email, e.phone_no AS Phone_no, e.tickets AS no_tickets, e.closed_tickets AS closed_tickets
                         FROM employee e
                         INNER JOIN roles r ON r.roles_id = e.roles_id
                         WHERE (@EmployeeId IS NULL OR e.employee_id LIKE @EmployeeId)
                            AND (@Role IS NULL OR r.roles_type LIKE @Role)
                            AND (@Name IS NULL OR e.name LIKE @Name)
                            AND (@Email IS NULL OR e.email LIKE @Email)
                            AND (@Phone_no IS NULL OR e.phone_no LIKE @Phone_no)
                            AND (@no_tickets IS NULL OR e.tickets LIKE @no_tickets)
                            AND (@closed_tickets IS NULL OR e.closed_tickets LIKE @closed_tickets)";

                List<Employee> employees = connection.Query<Employee>(query, new
                {
                    EmployeeId = string.IsNullOrEmpty(employeeId) ? null : "%" + employeeId + "%",
                    Role = string.IsNullOrEmpty(role) ? null : "%" + role + "%",
                    Name = string.IsNullOrEmpty(name) ? null : "%" + name + "%",
                    Email = string.IsNullOrEmpty(email) ? null : "%" + email + "%",
                    Phone_no = string.IsNullOrEmpty(phoneNumber) ? null : "%" + phoneNumber + "%",
                    no_tickets = string.IsNullOrEmpty(numTickets) ? null : "%" + numTickets + "%",
                    closed_tickets = string.IsNullOrEmpty(numclosed_tickets) ? null : "%" + numclosed_tickets + "%"
                }).ToList();

                return View("EmployeeList", employees);
            }
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
        //private int GetNextLeaveId()
        //{
        //    int leaveid = 0;
        //    using (SqlConnection connection = new SqlConnection(GetConnectionString()))
        //    {
        //        connection.Open();
        //        string query = $"SELECT MAX(leave_id) FROM leave";

        //        List<int> id = connection.Query<int>(query).AsList();
        //        foreach (int lid in id)
        //        {
        //            leaveid = lid;
        //        }

        //        return leaveid;
        //    }
        //}

        public IActionResult ApplyLeave()
        {
            //int nextLeaveId = GetNextLeaveId();

            //ViewBag.NextLeaveId = nextLeaveId;

            return View();
        }

        [HttpPost]
        public IActionResult ApplyLeave(EmployeeSchedule leave)
        {
            int leaveId;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                // Find the maximum leave_id from the database
                string maxIdQuery = @"SELECT MAX(leave_id) FROM leave";
                connection.Open();
                var maxId = connection.QuerySingleOrDefault<int?>(maxIdQuery);
                leaveId = maxId.HasValue ? maxId.Value + 1 : 1;

                // Retrieve the logged-in employee's ID
                int employeeId = GetLoggedInEmployeeId();

                // Store the leave request in the database
                string insertQuery = @"
            INSERT INTO leave (leave_id, employee_id, startDate, end_date AS EndDate, reason, proof_provided, is_approved)
            VALUES (@LeaveId, @EmployeeId, @StartDate, @EndDate, @Reason, @ProofProvided, 'pending');";

                connection.Execute(insertQuery, new { LeaveId = leaveId, EmployeeId = employeeId, leave.StartDate, leave.EndDate, leave.Reason, leave.ProofProvided });
            }

            if (leaveId > 0)
            {
                // Redirect to the LeaveRequests page
                return RedirectToAction("LeaveRequests");
            }
            else
            {
                // Handle error scenario
                return View("Error");
            }
        }

        private int GetLoggedInEmployeeId()
        {
            // Retrieve the employee ID from the logged-in user's claims
            var claimsIdentity = (ClaimsIdentity)HttpContext.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            int employeeId = int.Parse(claim.Value);

            return employeeId;
        }




        public IActionResult LeaveRequests()
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                // Retrieve all leave requests
                string query = @"
            SELECT l.leave_id AS LeaveId, e.employee_id AS EmployeeId, e.name AS EmployeeName, l.startDate, l.end_date AS EndDate, l.reason, l.is_approved, l.proof_provided
            FROM leave l
            INNER JOIN employee e ON e.employee_id = l.employee_id;";

                connection.Open();

                List<EmployeeSchedule> leaveRequests = connection.Query<EmployeeSchedule>(query).ToList();

                return View(leaveRequests);
            }
        }


        public IActionResult ReviewLeave(int leaveId)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string query = @"SELECT l.leave_id AS LeaveId, e.employee_id AS EmployeeId, e.name AS EmployeeName, l.startDate, l.end_date as EndDate, l.reason, l.is_approved, l.proof_provided
                FROM leave l
                INNER JOIN employee e ON e.employee_id = l.employee_id
                WHERE l.leave_id = @LeaveId;";

                connection.Open();

                EmployeeSchedule leaveRequest = connection.QueryFirstOrDefault<EmployeeSchedule>(query, new { LeaveId = leaveId });

                if (leaveRequest != null)
                {
                    return View(leaveRequest);
                }
            }

            return RedirectToAction("LeaveRequests");
        }

        [HttpPost]
        public IActionResult ReviewLeave(EmployeeSchedule leaveRequest)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string updateQuery = @"UPDATE leave
                                      SET is_approved = @IsApproved
                                      WHERE leave_id = @LeaveId;";

                connection.Open();

                connection.Execute(updateQuery, leaveRequest);
            }

            return RedirectToAction("LeaveRequests");
        }


        public IActionResult DownloadProof(int leaveId)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string query = "SELECT proof_provided FROM [leave] WHERE leave_id = @LeaveId;";
                connection.Open();
                string proofProvided = connection.QuerySingleOrDefault<string>(query, new { LeaveId = leaveId });

                if (!string.IsNullOrEmpty(proofProvided))
                {
                    byte[] pdfBytes = Convert.FromBase64String(proofProvided);
                    return File(pdfBytes, "application/pdf");
                }
            }

            return NotFound();
        }











        //for admin to add?
        public IActionResult NewEmployee()
        {
            return View();
        }

        [HttpPost]
        public IActionResult NewEmployee(NewEmployee newEmp)
        {
            if (!ModelState.IsValid)
            {
                //Validation Check
                ViewData["MsgType"] = "danger";
                return View("NewEmployee");
            }
            else
            {
                // Save the add employee data to the database
                string[] NumArray = newEmp.Email.Split("@");
                string numbers = NumArray[0];
                int EmpID = int.Parse(numbers);
                int totalDigits = EmpID.ToString().Length;

                using (var connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    NewEmployee helpdesk_agent = new NewEmployee
                    {
                        Employee_id = EmpID,
                        Roles_id = 3,
                        EmpPw = "helpdeskagent"+EmpID.ToString(),
                        Name = newEmp.Name,
                        Email = newEmp.Email,
                        Phone_no = newEmp.Phone_no,
                        Tickets = 0,
                        Last_login = null,
                        Closed_Tickets = 0,
                    };

                    NewEmployee support_eng = new NewEmployee
                    {
                        Employee_id = EmpID,
                        Roles_id = 4,
                        EmpPw = "supporteng" + EmpID.ToString(),
                        Name = newEmp.Name,
                        Email = newEmp.Email,
                        Phone_no = newEmp.Phone_no,
                        Tickets = 0,
                        Last_login = null,
                        Closed_Tickets = 0,
                    };

                    NewEmployee admin = new NewEmployee
                    {
                        Employee_id = EmpID,
                        Roles_id = 5,
                        EmpPw = "admin" + EmpID.ToString(),
                        Name = newEmp.Name,
                        Email = newEmp.Email,
                        Phone_no = newEmp.Phone_no,
                        Tickets = 0,
                        Last_login = null,
                        Closed_Tickets = 0,
                    };

                    if (totalDigits == 3)
                    {

                        string insertQuery1 = @"INSERT INTO employee (employee_id, roles_id, employee_pw, name, email, phone_no, tickets, last_login, closed_tickets)
                                            VALUES (@Employee_id, @roles_id, HASHBYTES('SHA1', @EmpPw), @Name, @Email, @Phone_no, @Tickets, @Last_login, @Closed_Tickets)";

                        if (connection.Execute(insertQuery1, helpdesk_agent) == 1)
                        {
                            TempData["Message"] = "Helpdesk Agent registered successfully";
                            TempData["MsgType"] = "success";
                        }
                        else
                        {
                            TempData["Message"] = "Account registered failed";
                            TempData["MsgType"] = "danger";
                        }
                    }

                    else if (totalDigits == 5)
                    {
                        string insertQuery2 = @"INSERT INTO employee (employee_id, roles_id, employee_pw, name, email, phone_no, tickets, last_login, closed_tickets)
                                            VALUES (@Employee_id, @roles_id, HASHBYTES('SHA1', @EmpPw), @Name, @Email, @Phone_no, @Tickets, @Last_login, @Closed_Tickets)";

                        if (connection.Execute(insertQuery2, admin) == 1)
                        {
                            TempData["Message"] = "Admin registered successfully";
                            TempData["MsgType"] = "success";
                        }
                        else
                        {
                            TempData["Message"] = "Account registered failed";
                            TempData["MsgType"] = "danger";
                        }
                    }

                    else if (totalDigits == 6)
                    {
                        string insertQuery3 = @"INSERT INTO employee (employee_id, roles_id, employee_pw, name, email, phone_no, tickets, last_login, closed_tickets)
                                            VALUES (@Employee_id, @roles_id, HASHBYTES('SHA1', @EmpPw), @Name, @Email, @Phone_no, @Tickets, @Last_login, @Closed_Tickets)";

                        if (connection.Execute(insertQuery3, support_eng) == 1)
                        {
                            TempData["Message"] = "Support Engineer registered successfully";
                            TempData["MsgType"] = "success";
                        }
                        else
                        {
                            TempData["Message"] = "Account registered failed";
                            TempData["MsgType"] = "danger";
                        }
                    }
                }
            }
            return RedirectToAction("EmployeeList", "Employee");
        }
    }
}
