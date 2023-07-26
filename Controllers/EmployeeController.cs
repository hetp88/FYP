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
                // Unauthorized actions for other roles
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

            // If employee not found, redirect back to the EmployeeList
            return RedirectToAction("EmployeeList");
        }
        [HttpPost]
        public IActionResult SaveEmployee(Employee updatedEmployee)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                // Fetch the current acc_status from the database
                string getStatusQuery = "SELECT acc_status FROM employee WHERE employee_id = @EmployeeId;";
                var currentStatus = connection.QueryFirstOrDefault<string>(getStatusQuery, new { EmployeeId = updatedEmployee.EmployeeId });

                // Update only the fields that were changed in the form
                string updateQuery = @"UPDATE employee 
                              SET name = @Name, email = @Email, phone_no = @Phone_no, 
                                  tickets = @no_tickets, closed_tickets = @closed_tickets,
                                  acc_status = @AccStatus
                              WHERE employee_id = @EmployeeId;";

                // If the acc_status was not changed in the form, keep the current status
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
                        // Retrieve all leave events for admin users
                        query = @"SELECT l.startDate, l.end_date AS EndDate, e.employee_id AS EmployeeId
                      FROM leave l
                      INNER JOIN employee e ON e.employee_id = l.employee_id
                      WHERE l.is_approved = 'approved';";
                    }
                    else
                    {
                        // Retrieve leave events for the logged-in staff member only
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

                    // Format the leave events for FullCalendar
                    var formattedEvents = leaveEvents.Select(e => new
                    {
                        title = e.EmployeeId,
                        start = e.StartDate.ToString("yyyy-MM-dd"),
                        end = e.EndDate.AddDays(1).ToString("yyyy-MM-dd") // Add 1 day to include the end date in the event
                    });

                    ViewBag.LeaveEvents = formattedEvents;

                    return View();
                }
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
            // Check if the logged-in user is an admin
            
        }

        public IActionResult ApplyLeave()
        {
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator"))
            {
                return View();
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
            //int nextLeaveId = GetNextLeaveId();

            //ViewBag.NextLeaveId = nextLeaveId;

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

                // Convert the proof provided to a byte array
                byte[] proofBytes;

                using (var memoryStream = new MemoryStream())
                {
                    leave.ProofProvided.CopyTo(memoryStream);
                    proofBytes = memoryStream.ToArray();
                }

                // Store the proof as a base64-encoded string
                string proof = Convert.ToBase64String(proofBytes);

                // Store the leave request in the database
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
                // Redirect to the LeaveRequests page
                return RedirectToAction("LeaveRequests");
            }
            else
            {
                // Handle error scenario
                return View("Error");
            }
        }


        public IActionResult DownloadProof(int leaveId)
        {
            int loggedInEmployeeId = GetLoggedInEmployeeId();

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                // Retrieve the employee ID and proof_provided for the given leaveId
                string query = "SELECT employee_id, proof_provided FROM [leave] WHERE leave_id = @LeaveId;";
                var leaveInfo = connection.QuerySingleOrDefault<dynamic>(query, new { LeaveId = leaveId });

                if (leaveInfo != null)
                {
                    int employeeId = leaveInfo.employee_id;
                    string proofProvided = leaveInfo.proof_provided;

                    // Check if the logged-in user is an administrator or the proof belongs to them
                    if (User.IsInRole("administrator") || loggedInEmployeeId == employeeId)
                    {
                        byte[] proofBytes = Convert.FromBase64String(proofProvided);
                        return File(proofBytes, "application/pdf");
                    }
                }
            }

            // If the logged-in user is not authorized to view the proof, return Forbidden page
            return View("Forbidden");
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
            if (User.IsInRole("administrator"))
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    // Retrieve all leave requests for administrators
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
                // For employees (other than administrators), filter leave requests based on the logged-in employee's ID
                int loggedInEmployeeId = GetLoggedInEmployeeId();

                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    // Retrieve leave requests for the logged-in employee only
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
                    // Retrieve the leave request for administrators
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
                // For employees (other than administrators), filter leave requests based on the logged-in employee's ID
                int loggedInEmployeeId = GetLoggedInEmployeeId();

                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    // Retrieve leave requests for the logged-in employee only
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
                    // For administrators, allow them to update the leave request status
                    string updateQuery = @"UPDATE leave
                                  SET is_approved = @IsApproved
                                  WHERE leave_id = @LeaveId;";

                    connection.Open();
                    connection.Execute(updateQuery, leaveRequest);
                }
                else
                {
                    // For employees (other than administrators), ensure they can only review their own leave requests
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
                            // Delete the leave request from the database
                            string deleteQuery = "DELETE FROM leave WHERE leave_id = @LeaveId AND employee_id = @EmployeeId;";
                            connection.Execute(deleteQuery, new { LeaveId = leaveRequest.LeaveId, EmployeeId = loggedInEmployeeId });

                            // Redirect back to the LeaveRequests page after successful withdrawal
                            return RedirectToAction("LeaveRequests");
                        }
                        else
                        {
                            // Update the leave information for the logged-in employee
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

                            // Redirect back to the LeaveRequests page after successful update
                            return RedirectToAction("LeaveRequests");
                        }
                    }
                    else
                    {
                        // Unauthorized access, redirect back to the LeaveRequests page
                        return RedirectToAction("LeaveRequests");
                    }
                }
            }

            // If the method reaches this point, there might be an error, redirect to the LeaveRequests page
            return RedirectToAction("LeaveRequests");
        }

        [HttpPost]
        public IActionResult UpdateLeaveDetails(EmployeeSchedule updatedLeave)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                // Prepare the base update query
                string updateQuery = @"UPDATE leave 
                              SET";

                // Initialize a list to store the update parameters
                var parameters = new DynamicParameters();
                parameters.Add("LeaveId", updatedLeave.LeaveId);

                // Check and add the fields that are updated by the employee
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

                // Check if the proof has been updated and add it to the update query
                if (updatedLeave.ProofProvided != null)
                {
                    // Convert the new proof provided to a byte array
                    byte[] proofBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        updatedLeave.ProofProvided.CopyTo(memoryStream);
                        proofBytes = memoryStream.ToArray();
                    }

                    // Store the proof as a base64-encoded string
                    string proof = Convert.ToBase64String(proofBytes);

                    // Add the proof_provided parameter to the update query
                    updateQuery += " proof_provided = @ProofProvided,";
                    parameters.Add("ProofProvided", proof);
                }

                // Remove the trailing comma
                updateQuery = updateQuery.TrimEnd(',');

                // Append the WHERE clause to update only the specific leave entry
                updateQuery += " WHERE leave_id = @LeaveId;";

                // Execute the update query with the parameters
                connection.Execute(updateQuery, parameters);
            }

            // Redirect to the LeaveRequests page or any other appropriate page after updating the leave details
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
                // Hash the password using BCrypt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newEmployee.Password);

                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();

                    // Find the largest employee ID for all roles
                    string maxEmployeeIdQuery = "SELECT MAX(employee_id) FROM employee;";
                    int maxEmployeeId = connection.QuerySingleOrDefault<int>(maxEmployeeIdQuery);

                    // Increment the employee ID by 1 to get the new employee's ID
                    int newEmployeeId = maxEmployeeId + 1;

                    // Assign the new employee ID to EmployeeId
                    newEmployee.EmployeeId = newEmployeeId;

                    // Insert the new employee into the database
                    string insertQuery = @"
                INSERT INTO employee (employee_id, roles_id, name, email, phone_no, employee_pw, tickets, closed_tickets)
                VALUES (@EmployeeId, @RolesId, @Name, @Email, @PhoneNo, @Password, 0, 0);";

                    connection.Execute(insertQuery, new
                    {
                        newEmployee.EmployeeId,
                        newEmployee.RolesId,
                        newEmployee.Name,
                        newEmployee.Email,
                        PhoneNo = newEmployee.PhoneNo.ToString(),
                        Password = Encoding.Unicode.GetBytes(hashedPassword)
                    });
                }

                // Redirect to the EmployeeList page after successfully adding the new employee
                return RedirectToAction("EmployeeList");
            }

            // If the model state is invalid, return the view with validation errors
            return View("NewEmployee", newEmployee);
        }



    }
}