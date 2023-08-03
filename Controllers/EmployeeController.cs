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
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography;
using BCrypt.Net;

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
            if (User.IsInRole("administrator"))
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    string query = @"SELECT e.employee_id AS EmployeeId, 
                r.roles_type AS Role, e.name, e.email, e.phone_no,
                e.tickets AS no_tickets, e.closed_tickets AS closed_tickets,
                e.acc_status AS AccStatus
                FROM employee e
                INNER JOIN roles r ON r.roles_id = e.roles_id;";

                    connection.Open();

                    List<Employee> employees = connection.Query<Employee>(query).ToList();

                    return View(employees);
                }
            }
            else
            {
                return View("Forbidden");
            }

        }

        public IActionResult UpdateEmployee(int employeeId)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string query = @"SELECT e.employee_id AS EmployeeId, r.roles_type AS Role, e.name, e.email, e.phone_no, 
                        e.tickets AS no_tickets, e.closed_tickets AS closed_tickets, e.acc_status AS AccStatus
                        FROM employee e
                        INNER JOIN roles r ON r.roles_id = e.roles_id
                        WHERE e.employee_id = @EmployeeId;";

                var employee = connection.QueryFirstOrDefault<Employee>(query, new { EmployeeId = employeeId });

                if (employee != null)
                {
                    return View(employee);
                }
            }
            return RedirectToAction("EmployeeList");
        }

        [HttpPost]
        public IActionResult SaveEmployee(Employee updatedEmployee)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();
                string getStatusQuery = "SELECT acc_status FROM employee WHERE employee_id = @EmployeeId;";
                var currentStatus = connection.QueryFirstOrDefault<string>(getStatusQuery, new { EmployeeId = updatedEmployee.EmployeeId });
                string updateQuery = @"UPDATE employee 
                              SET name = @Name, email = @Email, phone_no = @Phone_no, 
                                  tickets = @no_tickets, closed_tickets = @closed_tickets,
                                  acc_status = @AccStatus
                              WHERE employee_id = @EmployeeId;";

                if (string.IsNullOrWhiteSpace(updatedEmployee.AccStatus))
                {
                    updatedEmployee.AccStatus = currentStatus;
                }

                connection.Execute(updateQuery, new
                {
                    EmployeeId = updatedEmployee.EmployeeId,
                    Name = updatedEmployee.Name,
                    Email = updatedEmployee.Email,
                    Phone_no = updatedEmployee.Phone_no,
                    no_tickets = updatedEmployee.no_tickets,
                    closed_tickets = updatedEmployee.closed_tickets,
                    AccStatus = updatedEmployee.AccStatus
                });
            }

            return RedirectToAction("EmployeeList");
        }



        public IActionResult SearchEmployees(string employeeId, string role, string name, string email, string phoneNumber, string numTickets, string numclosed_tickets, string accStatus)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string query = @"
            SELECT e.employee_id AS EmployeeId, r.roles_type AS Role, e.name, e.email, e.phone_no AS Phone_no, 
                   e.tickets AS no_tickets, e.closed_tickets AS closed_tickets, e.acc_status AS AccStatus
            FROM employee e
            INNER JOIN roles r ON r.roles_id = e.roles_id
            WHERE (@EmployeeId IS NULL OR e.employee_id LIKE @EmployeeId)
                AND (@Role IS NULL OR r.roles_type LIKE @Role)
                AND (@Name IS NULL OR e.name LIKE @Name)
                AND (@Email IS NULL OR e.email LIKE @Email)
                AND (@Phone_no IS NULL OR e.phone_no LIKE @Phone_no)
                AND (@no_tickets IS NULL OR e.tickets LIKE @no_tickets)
                AND (@closed_tickets IS NULL OR e.closed_tickets LIKE @closed_tickets)
                AND (@AccStatus IS NULL OR e.acc_status LIKE @AccStatus)";

                List<Employee> employees = connection.Query<Employee>(query, new
                {
                    EmployeeId = string.IsNullOrEmpty(employeeId) ? null : "%" + employeeId + "%",
                    Role = string.IsNullOrEmpty(role) ? null : "%" + role + "%",
                    Name = string.IsNullOrEmpty(name) ? null : "%" + name + "%",
                    Email = string.IsNullOrEmpty(email) ? null : "%" + email + "%",
                    Phone_no = string.IsNullOrEmpty(phoneNumber) ? null : "%" + phoneNumber + "%",
                    no_tickets = string.IsNullOrEmpty(numTickets) ? null : "%" + numTickets + "%",
                    closed_tickets = string.IsNullOrEmpty(numclosed_tickets) ? null : "%" + numclosed_tickets + "%",
                    AccStatus = string.IsNullOrEmpty(accStatus) ? null : "%" + accStatus + "%"
                }).ToList();

                return View("EmployeeList", employees);
            }
        }



        public IActionResult SearchLeaveRequests(string employeeId, DateTime? startDate, DateTime? endDate, string reason, string status)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string query = @"
        SELECT l.leave_id AS LeaveId, e.employee_id AS EmployeeId, e.name AS EmployeeName, l.startDate AS StartDate, l.end_date AS EndDate, l.reason, l.is_approved AS IsApproved, l.proof_provided
        FROM leave l
        INNER JOIN employee e ON e.employee_id = l.employee_id
        WHERE (@EmployeeId IS NULL OR e.employee_id LIKE @EmployeeId)
            AND (@StartDate IS NULL OR l.startDate = @StartDate)
            AND (@EndDate IS NULL OR l.end_date = @EndDate)
            AND (@Reason IS NULL OR l.reason LIKE @Reason)
            AND (@Status IS NULL OR l.is_approved = @Status);";

                List<EmployeeSchedule> leaveRequests = connection.Query<EmployeeSchedule>(query, new
                {
                    EmployeeId = string.IsNullOrEmpty(employeeId) ? null : "%" + employeeId + "%",
                    StartDate = startDate,
                    EndDate = endDate,
                    Reason = string.IsNullOrEmpty(reason) ? null : "%" + reason + "%",
                    Status = string.IsNullOrEmpty(status) ? null : status
                }).ToList();

                return View("LeaveRequests", leaveRequests);
            }
        }

        public IActionResult Schedule()
        {
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator"))
            {
                bool isAdmin = User.IsInRole("administrator");

                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    string query;
                    if (isAdmin)
                    {
                        query = @"SELECT l.startDate, l.end_date AS EndDate, e.employee_id AS EmployeeId
                      FROM leave l
                      INNER JOIN employee e ON e.employee_id = l.employee_id
                      WHERE l.is_approved = 'approved';";
                    }
                    else
                    {
                        int employeeId = GetLoggedInEmployeeId();
                        query = @"SELECT l.startDate, l.end_date AS EndDate, e.employee_id AS EmployeeId
                      FROM leave l
                      INNER JOIN employee e ON e.employee_id = l.employee_id
                      WHERE l.is_approved = 'approved' AND e.employee_id = @EmployeeId;";
                    }

                    connection.Open();

                    List<EmployeeSchedule> leaveEvents;
                    if (isAdmin)
                    {
                        leaveEvents = connection.Query<EmployeeSchedule>(query).ToList();
                    }
                    else
                    {
                        leaveEvents = connection.Query<EmployeeSchedule>(query, new { EmployeeId = GetLoggedInEmployeeId() }).ToList();
                    }
                    var formattedEvents = leaveEvents.Select(e => new
                    {
                        title = e.EmployeeId,
                        start = e.StartDate.ToString("yyyy-MM-dd"),
                        end = e.EndDate.AddDays(1).ToString("yyyy-MM-dd") 
                    });

                    ViewBag.LeaveEvents = formattedEvents;

                    return View();
                }
            }
            else
            {
                return View("Forbidden");
            }
            
        }

        public IActionResult ApplyLeave()
        {
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator"))
            {
                return View();
            }
            else
            {
                return View("Forbidden");
            }

        }

        [HttpPost]
        public IActionResult ApplyLeave(EmployeeSchedule leave)
        {
            int leaveId;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string maxIdQuery = @"SELECT MAX(leave_id) FROM leave";
                connection.Open();
                var maxId = connection.QuerySingleOrDefault<int?>(maxIdQuery);
                leaveId = maxId.HasValue ? maxId.Value + 1 : 1;

                int employeeId = GetLoggedInEmployeeId();
                byte[] proofBytes;

                using (var memoryStream = new MemoryStream())
                {
                    leave.ProofProvided.CopyTo(memoryStream);
                    proofBytes = memoryStream.ToArray();
                }
                string proof = Convert.ToBase64String(proofBytes);
                string insertQuery = @"
            INSERT INTO leave (leave_id, employee_id, startDate, end_date, reason, proof_provided, is_approved)
            VALUES (@LeaveId, @EmployeeId, @StartDate, @EndDate, @Reason, @ProofProvided, 'Pending');";

                connection.Execute(insertQuery, new
                {
                    LeaveId = leaveId,
                    EmployeeId = employeeId,
                    leave.StartDate,
                    leave.EndDate,
                    leave.Reason,
                    ProofProvided = proof
                });
            }

            if (leaveId > 0)
            {
                return RedirectToAction("LeaveRequests");
            }
            else
            {
                return View("Error");
            }
        }


        public IActionResult DownloadProof(int leaveId)
        {
            int loggedInEmployeeId = GetLoggedInEmployeeId();

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                string query = "SELECT employee_id, proof_provided FROM [leave] WHERE leave_id = @LeaveId;";
                var leaveInfo = connection.QuerySingleOrDefault<dynamic>(query, new { LeaveId = leaveId });

                if (leaveInfo != null)
                {
                    int employeeId = leaveInfo.employee_id;
                    string proofProvided = leaveInfo.proof_provided;

                    if (User.IsInRole("administrator") || loggedInEmployeeId == employeeId)
                    {
                        byte[] proofBytes = Convert.FromBase64String(proofProvided);
                        return File(proofBytes, "application/pdf");
                    }
                }
            }
            return View("Forbidden");
        }



        private int GetLoggedInEmployeeId()
        {
            var claimsIdentity = (ClaimsIdentity)HttpContext.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            int employeeId = int.Parse(claim.Value);

            return employeeId;
        }

        public IActionResult LeaveRequests()
        {
            if (User.IsInRole("administrator"))
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    string query = @"
                SELECT l.leave_id AS LeaveId, e.employee_id AS EmployeeId, e.name AS EmployeeName, l.startDate AS StartDate, l.end_date AS EndDate, l.reason, l.is_approved AS IsApproved, l.proof_provided
                FROM leave l
                INNER JOIN employee e ON e.employee_id = l.employee_id;";

                    connection.Open();

                    List<EmployeeSchedule> leaveRequests = connection.Query<EmployeeSchedule>(query).ToList();

                    return View(leaveRequests);
                }
            }
            else
            {
                int loggedInEmployeeId = GetLoggedInEmployeeId();

                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    string query = @"
                SELECT l.leave_id AS LeaveId, e.employee_id AS EmployeeId, e.name AS EmployeeName, l.startDate AS StartDate, l.end_date AS EndDate, l.reason, l.is_approved AS IsApproved, l.proof_provided
                FROM leave l
                INNER JOIN employee e ON e.employee_id = l.employee_id
                WHERE e.employee_id = @LoggedInEmployeeId;";

                    connection.Open();

                    List<EmployeeSchedule> leaveRequests = connection.Query<EmployeeSchedule>(query, new { LoggedInEmployeeId = loggedInEmployeeId }).ToList();

                    return View(leaveRequests);
                }
            }
        }
        public IActionResult ReviewLeave(int leaveId)
        {
            if (User.IsInRole("administrator"))
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    string query = @"
                SELECT l.leave_id AS LeaveId, e.employee_id AS EmployeeId, e.name AS EmployeeName, l.startDate, l.end_date as EndDate, l.reason, l.is_approved, l.proof_provided
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
            else
            {
                int loggedInEmployeeId = GetLoggedInEmployeeId();

                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    string query = @"
                SELECT l.leave_id AS LeaveId, e.employee_id AS EmployeeId, e.name AS EmployeeName, l.startDate, l.end_date as EndDate, l.reason, l.is_approved, l.proof_provided
                FROM leave l
                INNER JOIN employee e ON e.employee_id = l.employee_id
                WHERE l.leave_id = @LeaveId AND e.employee_id = @LoggedInEmployeeId;";

                    connection.Open();

                    EmployeeSchedule leaveRequest = connection.QueryFirstOrDefault<EmployeeSchedule>(query, new { LeaveId = leaveId, LoggedInEmployeeId = loggedInEmployeeId });

                    if (leaveRequest != null)
                    {
                        return View(leaveRequest);
                    }
                }

                return RedirectToAction("LeaveRequests");
            }
        }

        [HttpPost]
        public IActionResult ReviewLeave(EmployeeSchedule leaveRequest)
        {
            int loggedInEmployeeId = GetLoggedInEmployeeId();

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                if (User.IsInRole("administrator"))
                {
                    string updateQuery = @"UPDATE leave
                                  SET is_approved = @IsApproved
                                  WHERE leave_id = @LeaveId;";

                    connection.Open();
                    connection.Execute(updateQuery, leaveRequest);
                }
                else
                {
                    string query = @"
                SELECT e.employee_id AS EmployeeId, l.is_approved
                FROM leave l
                INNER JOIN employee e ON e.employee_id = l.employee_id
                WHERE l.leave_id = @LeaveId AND e.employee_id = @LoggedInEmployeeId;";

                    connection.Open();
                    EmployeeSchedule existingLeaveRequest = connection.QueryFirstOrDefault<EmployeeSchedule>(query, new { LeaveId = leaveRequest.LeaveId, LoggedInEmployeeId = loggedInEmployeeId });

                    if (existingLeaveRequest != null)
                    {
                        if (leaveRequest.IsApproved == "Withdraw")
                        {
                            string deleteQuery = "DELETE FROM leave WHERE leave_id = @LeaveId AND employee_id = @EmployeeId;";
                            connection.Execute(deleteQuery, new { LeaveId = leaveRequest.LeaveId, EmployeeId = loggedInEmployeeId });
                            return RedirectToAction("LeaveRequests");
                        }
                        else
                        {
                            string updateQuery = @"UPDATE leave
                                          SET startDate = @StartDate, end_date = @EndDate, reason = @Reason
                                          WHERE leave_id = @LeaveId AND employee_id = @EmployeeId;";

                            connection.Execute(updateQuery, new
                            {
                                LeaveId = leaveRequest.LeaveId,
                                EmployeeId = loggedInEmployeeId,
                                StartDate = leaveRequest.StartDate,
                                EndDate = leaveRequest.EndDate,
                                Reason = leaveRequest.Reason
                            });
                            return RedirectToAction("LeaveRequests");
                        }
                    }
                    else
                    {
                        return RedirectToAction("LeaveRequests");
                    }
                }
            }
            return RedirectToAction("LeaveRequests");
        }

        [HttpPost]
        public IActionResult UpdateLeaveDetails(EmployeeSchedule updatedLeave)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();
                string updateQuery = @"UPDATE leave 
                              SET";
                var parameters = new DynamicParameters();
                parameters.Add("LeaveId", updatedLeave.LeaveId);

                if (updatedLeave.StartDate != default(DateTime))
                {
                    updateQuery += " startDate = @StartDate,";
                    parameters.Add("StartDate", updatedLeave.StartDate);
                }

                if (updatedLeave.EndDate != default(DateTime))
                {
                    updateQuery += " end_date = @EndDate,";
                    parameters.Add("EndDate", updatedLeave.EndDate);
                }

                if (!string.IsNullOrEmpty(updatedLeave.Reason))
                {
                    updateQuery += " reason = @Reason,";
                    parameters.Add("Reason", updatedLeave.Reason);
                }

                if (updatedLeave.ProofProvided != null)
                {
                    byte[] proofBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        updatedLeave.ProofProvided.CopyTo(memoryStream);
                        proofBytes = memoryStream.ToArray();
                    }

                    string proof = Convert.ToBase64String(proofBytes);
                    updateQuery += " proof_provided = @ProofProvided,";
                    parameters.Add("ProofProvided", proof);
                }
                updateQuery = updateQuery.TrimEnd(',');
                updateQuery += " WHERE leave_id = @LeaveId;";

                connection.Execute(updateQuery, parameters);
            }
            return RedirectToAction("LeaveRequests");
        }

        public IActionResult NewEmployee()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateNewEmployee(NewEmployeeViewModel newEmployee)
        {
            if (ModelState.IsValid)
            {

                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    byte[] passwordBytes = Encoding.UTF8.GetBytes(newEmployee.Password);
                    byte[] hashedPasswordBytes = SHA1.Create().ComputeHash(passwordBytes);
                    string hashedPassword = "0x" + BitConverter.ToString(hashedPasswordBytes).Replace("-", "");
                    string maxEmployeeIdQuery = "SELECT MAX(employee_id) FROM employee WHERE roles_id = @RolesId;";
                    int maxEmployeeId = connection.QuerySingleOrDefault<int>(maxEmployeeIdQuery, new { RolesId = newEmployee.RolesId });

                    int newEmployeeId = maxEmployeeId + 1;

                    string formattedEmployeeId;
                    switch (newEmployee.RolesId)
                    {
                        case 3: 
                            formattedEmployeeId = (newEmployeeId).ToString();
                            break;
                        case 4: 
                            formattedEmployeeId = (newEmployeeId).ToString();
                            break;
                        case 5: 
                            formattedEmployeeId = (newEmployeeId).ToString();
                            break;
                        default:
                            formattedEmployeeId = newEmployeeId.ToString();
                            break;
                    }

                    string insertQuery = $"INSERT INTO employee (employee_id, roles_id, name, email, phone_no, employee_pw, tickets, closed_tickets, acc_status) VALUES (@EmployeeId, @RolesId, @Name, @Email, @PhoneNo, {hashedPassword}, 0, 0, 'active');";

                    connection.Execute(insertQuery, new
                    {
                        EmployeeId = formattedEmployeeId,
                        newEmployee.RolesId,
                        newEmployee.Name,
                        newEmployee.Email,
                        PhoneNo = newEmployee.PhoneNo.ToString(),
                        newEmployee.Password,
                    });
                }
                return RedirectToAction("EmployeeList");
            }

            return View("NewEmployee", newEmployee);
        }




    }
}


