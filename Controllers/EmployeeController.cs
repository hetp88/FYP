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
                string query = @"SELECT e.employee_id AS EmployeeId, r.roles_type AS Role, e.name, e.email, e.phone_no, e.tickets AS no_tickets
                FROM employee e
                INNER JOIN roles r ON r.roles_id = e.roles_id;";

                connection.Open();

                List<Employee> employees = connection.Query<Employee>(query).ToList();

                return View(employees);
            }
        }
        public IActionResult SearchEmployees(string employeeId, string role, string name, string email, string phoneNumber, string numTickets)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string query = @"SELECT e.employee_id AS EmployeeId, r.roles_type AS Role, e.name, e.email, e.phone_no AS Phone_no, e.tickets AS no_tickets
                         FROM employee e
                         INNER JOIN roles r ON r.roles_id = e.roles_id
                         WHERE (@EmployeeId IS NULL OR e.employee_id LIKE @EmployeeId)
                            AND (@Role IS NULL OR r.roles_type LIKE @Role)
                            AND (@Name IS NULL OR e.name LIKE @Name)
                            AND (@Email IS NULL OR e.email LIKE @Email)
                            AND (@Phone_no IS NULL OR e.phone_no LIKE @Phone_no)
                            AND (@no_tickets IS NULL OR e.tickets LIKE @no_tickets)";

                List<Employee> employees = connection.Query<Employee>(query, new
                {
                    EmployeeId = string.IsNullOrEmpty(employeeId) ? null : "%" + employeeId + "%",
                    Role = string.IsNullOrEmpty(role) ? null : "%" + role + "%",
                    Name = string.IsNullOrEmpty(name) ? null : "%" + name + "%",
                    Email = string.IsNullOrEmpty(email) ? null : "%" + email + "%",
                    Phone_no = string.IsNullOrEmpty(phoneNumber) ? null : "%" + phoneNumber + "%",
                    no_tickets = string.IsNullOrEmpty(numTickets) ? null : "%" + numTickets + "%"
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
            //int nextLeaveId = GetNextLeaveId();
            int leaveid = 0;
            int eid = 0;

            string currentemp = Environment.UserName;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string idquery = $"SELECT MAX(leave_id) FROM leave";
                string empquery = $"SELECT employee_id AS EmployeeId FROM employee WHERE name='{currentemp}'";

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

                string insertQuery = @"INSERT INTO leave (employee_id, leave_id, startDate, end_date, reason, proof_provided, is_approved) 
                                        VALUES (@EmployeeId, @LeaveId, @StartDate, @EndDate, @Reason, @ProofProvided, @IsApproved)";

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
                    };

                    if (totalDigits == 3)
                    {

                        string insertQuery1 = @"INSERT INTO employee (employee_id, roles_id, employee_pw, name, email, phone_no, tickets)
                                            VALUES (@Employee_id, @roles_id, HASHBYTES('SHA1', @EmpPw), @Name, @Email, @Phone_no, @Tickets)";

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
                        string insertQuery2 = @"INSERT INTO employee (employee_id, roles_id, employee_pw, name, email, phone_no, tickets)
                                            VALUES (@Employee_id, @roles_id, HASHBYTES('SHA1', @EmpPw), @Name, @Email, @Phone_no, @Tickets)";

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
                        string insertQuery3 = @"INSERT INTO employee (employee_id, roles_id, employee_pw, name, email, phone_no, tickets)
                                            VALUES (@Employee_id, @roles_id, HASHBYTES('SHA1', @EmpPw), @Name, @Email, @Phone_no, @Tickets)";

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
