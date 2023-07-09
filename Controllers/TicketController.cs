using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using FYP.Models;
using Dapper;
using System.Security.Cryptography;
using System;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace FYP.Controllers
{
    public class TicketController : Controller
    {
        private readonly string _connectionString;

        private readonly IHttpContextAccessor contextAccessor;

        public TicketController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            contextAccessor = httpContextAccessor;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult UserTicket()
        {
            //GET CURRENT USER ID
            int? currentuser = contextAccessor.HttpContext.Session.GetInt32("userID");
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = $"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution FROM ticket t INNER JOIN users u ON u.userid = t.userid INNER JOIN ticket_categories tc ON tc.category_id = t.category_id INNER JOIN employee e ON t.employee_id = e.employee_id  WHERE t.userid='{currentuser}'";

                connection.Open();
                List<Ticket> tickets = connection.Query<Ticket>(query).AsList();
                return View(tickets);
            }
        }

        public IActionResult ViewTicket()
        {
            // Retrieve ticket data from the database

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, 
                                       t.datetime, t.priority, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution, t.escalate_reason
                                FROM ticket t
                                INNER JOIN users u ON u.userid = t.userid
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON t.employee_id = e.employee_id;";

                connection.Open();
                List<Ticket> tickets = connection.Query<Ticket>(query).AsList();
                return View(tickets);
            }
        }

        public IActionResult SearchTicket(string ticketIdQuery, string userIdQuery, string ticketTypeQuery, string statusQuery, string priorityQuery, string employeeQuery)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, 
                                       t.datetime, t.priority, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution, t.escalate_reason
                                FROM ticket t
                                INNER JOIN users u ON u.userid = t.userid
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON t.employee_id = e.employee_id
                                WHERE (@TicketIdQuery IS NULL OR t.ticket_id = @TicketIdQuery)
                                AND (@UserIdQuery IS NULL OR t.userid = @UserIdQuery) 
                                AND (@TicketTypeQuery IS NULL OR t.type = @TicketTypeQuery)
                                AND (@StatusQuery IS NULL OR t.status = @StatusQuery)               
                                AND (@PriorityQuery IS NULL OR t.priority = @PriorityQuery)               
                                AND (@EmployeeQuery IS NULL OR e.name = @EmployeeQuery)";

                connection.Open();

                List<Ticket> tickets = connection.Query<Ticket>(query, new
                {
                    TicketIdQuery = ticketIdQuery,
                    UserIdQuery = userIdQuery,
                    TicketTypeQuery = ticketTypeQuery,                  
                    StatusQuery = statusQuery,                    
                    PriorityQuery = priorityQuery,
                    EmployeeQuery = employeeQuery
                }).AsList();

                return View("ViewTicket", tickets);
            }
        }

        public IActionResult AddTicket()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddTicket(Ticket ticket)
        {
            int ticketid = 0;
            Random generate = new Random();
            int assignedticket = 0;
            DateTime created = DateTime.Now;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string idQuery = @"SELECT MAX(ticket_id) FROM ticket";

                string empquery = @"SELECT e.employee_id
                                    FROM employee e
                                    LEFT JOIN [leave] l ON e.employee_id = l.employee_id
                                    WHERE e.roles_id = 3
                                      AND (e.tickets IS NULL OR e.tickets < 5)
                                      AND (l.is_approved = 'Rejected' OR l.is_approved IS NULL)
                                      AND ((GETDATE() NOT BETWEEN l.startDate AND l.end_date)
  	                                    OR (GETDATE() <> l.startDate AND GETDATE() <> l.end_date) 
                                        OR l.employee_id IS NULL)";

                connection.Open();

                List<int> tid = connection.Query<int>(idQuery).AsList();
                foreach (int id in tid)
                {
                    ticketid = id;
                }

                List<int> empid = connection.Query<int>(empquery).AsList();
                int emp = generate.Next(0, empid.Count);

                Ticket newTicket = new Ticket
                {
                    TicketId = ticketid + 1,
                    UserId = ticket.UserId,
                    Type = ticket.Type,
                    Description = ticket.Description,
                    Category = ticket.Category,
                    Status = "submitted",
                    DateTime = Convert.ToDateTime(created),
                    Priority = ticket.Priority,
                    Employee = empid[emp],
                    DevicesInvolved = ticket.DevicesInvolved,
                    Additional_Details = ticket.Additional_Details,
                    Resolution = null,
                    Escalate_Reason = null,
                };

                //Console.WriteLine(empid[emp]);
                int eid = empid[emp];

                string empticket = $"SELECT tickets FROM employee WHERE employee_id = '{eid}'";

                List<int> eticket = connection.Query<int>(empticket).AsList();
                foreach (int id in eticket)
                {
                    assignedticket = id + 1;
                }

                string query = @"INSERT INTO ticket (ticket_id, userid, type, description, category_id, status, datetime, priority, employee_id, devices_involved, additional_details, resolution, escalate_reason) 
                                    VALUES (@TicketId, @UserId, @Type, @Description, @Category, @Status, @DateTime, @Priority, @Employee, @DevicesInvolved, @Additional_Details, @Resolution, @Escalate_Reason)";

                string update = $"UPDATE employee SET tickets = '{assignedticket}' WHERE employee_id = '{eid}'";

                if (connection.Execute(query, newTicket) == 1 && connection.Execute(update) == 1)
                {
                    ViewData["Message"] = "Ticket submitted successfully";
                    ViewData["MsgType"] = "success";
                }
                else
                {
                    TempData["Message"] = "Ticket submit failed";
                    TempData["MsgType"] = "danger";
                }
            }
            return RedirectToAction("ViewTicket", "Ticket");
        }

        public IActionResult EscalateTicket(int ticketid)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string tquery = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority,
                                         t.employee_id AS Employee, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution
                                FROM ticket t 
                                INNER JOIN users u ON u.userid = t.userid 
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON t.employee_id = e.employee_id
                                WHERE t.ticket_id = @TicketId;";

                connection.Open();

                //Ticket leaveRequest = connection.QueryFirstOrDefault<Ticekt>(query, new { LeaveId = leaveId });

                Ticket tickets = connection.QueryFirstOrDefault<Ticket>(tquery, new { TicketId = ticketid });

                if (tickets != null)
                {
                    return View(tickets);
                }
            }
            return RedirectToAction("ViewTicket");
        }

        [HttpPost]
        public IActionResult EscalateTicket(Ticket ticket)
        {
            Random generate = new Random();
            int assignedticket = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string equery = @"SELECT e.employee_id
                                    FROM employee e
                                    LEFT JOIN [leave] l ON e.employee_id = l.employee_id
                                    WHERE e.roles_id = 4
                                        AND (e.tickets IS NULL OR e.tickets < 5)
                                        AND (l.is_approved = 'Rejected' OR l.is_approved IS NULL)
                                        AND ((GETDATE() NOT BETWEEN l.startDate AND l.end_date)
  	                                    OR (GETDATE() <> l.startDate AND GETDATE() <> l.end_date) 
                                        OR l.employee_id IS NULL)";

                List<int> emid = connection.Query<int>(equery).AsList();
                int emp = generate.Next(0, emid.Count);

                connection.Open();

                Ticket escalation = new Ticket
                {
                    TicketId = ticket.TicketId,
                    Status = "escalated",
                    Employee = emid[emp],
                    Escalate_Reason = ticket.Escalate_Reason,
                };

                string update = @"UPDATE Ticket SET status = @Status escalation_SE = @Employee, escalate_reason = @Escalate_Reason WHERE ticket_id = @TicketId";

                string empticket = $"SELECT tickets FROM employee WHERE employee_id = '{emid[emp]}'";

                List<int> eticket = connection.Query<int>(empticket).AsList();
                
                foreach (int id in eticket)
                {
                    assignedticket = id + 1;
                }

                if (connection.Execute(update, escalation) == 1 && connection.Execute(empticket) == 1)
                {
                    ViewData["Message"] = "Escalated successfully.";
                }
                else
                {
                    ViewData["Message"] = "Unsuccessful escalate. Do try again.";
                }
            }
            return RedirectToAction("ViewTicket", "Ticket");
        }

        public IActionResult HAUpdateTicket(int ticketid)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string tquery = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority,
                                         t.employee_id AS Employee, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution, t.escalation_SE AS Escalate_SE, t.escalate_reason
                                FROM ticket t 
                                INNER JOIN users u ON u.userid = t.userid 
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON t.employee_id = e.employee_id
                                WHERE t.ticket_id = @TicketId;";

                connection.Open();

                Ticket tickets = connection.QueryFirstOrDefault<Ticket>(tquery, new { TicketId = ticketid });

                if (tickets != null)
                {
                    return View(tickets);
                }
            }
            return View("ViewTicket");
        }

        [HttpPost]
        public IActionResult HAUpdateTicket(Ticket tickets)
        {
            int HAassigned = 0;
            int SEassigned = 0;
            int HAclosed = 0;
            int SEclosed = 0;

            string updateticket = "";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                Ticket tupdate = new Ticket()
                {
                    TicketId = tickets.TicketId,
                    newStatus = tickets.newStatus,
                    Resolution = tickets.Resolution,
                };

                if (tickets.newStatus == "resolved")
                {
                    //update ticket status, resolution
                    updateticket = @"UPDATE Ticket SET status = @newStatus, resolution = @Resolution WHERE ticket_id = @TicketId";
                }
                else
                {
                    updateticket = @"UPDATE Ticket SET status = @newStatus WHERE ticket_id = @TicketId";
                }

                //updating the number of assigned and closed tickets
                //assignedHAticket --> to minus of 1 from current number of tickets assgined for helpdesk agent if status is updated to 'closed'
                string assignedHAticket = $"SELECT tickets FROM employee WHERE employee_id = '{tickets.Employee}'";

                //closedHAticket --> to add 1 from current number of tickets closed for helpdesk agent if status is updated to 'closed'
                string closedHAticket = $"SELECT closed_tickets FROM employee WHERE employee_id = '{tickets.Employee}'";

                //assignedSEticket --> to minus of 1 from current number of tickets escalated/assigned for support engineer if status is updated to 'closed'
                string assignedSEticket = $"SELECT e.tickets" +
                                          $"FROM employee e " +
                                          $"INNER JOIN ticket t ON t.employee_id = e.employee_id " +
                                          $"WHERE e.employee_id = '{tickets.Escalate_SE}'";

                //closedSEticket --> to add 1 from current number of tickets closed for support engineer if status is updated to 'closed'
                string closedSEticket = $"SELECT e.closed_tickets" +
                                        $"FROM employee e " +
                                        $"INNER JOIN ticket t ON t.employee_id = e.employee_id " +
                                        $"WHERE e.employee_id = '{tickets.Escalate_SE}'";

                //
                if (tickets.newStatus == "closed" && tickets.Escalate_Reason == null)
                {
                    List<int> HAassignedticket = connection.Query<int>(assignedHAticket).AsList();
                    foreach (int id in HAassignedticket)
                    {
                        HAassigned = id - 1;
                    }

                    List<int> HAclosedticket = connection.Query<int>(closedHAticket).AsList();
                    foreach (int id in HAclosedticket)
                    {
                        HAclosed = id + 1;
                    }

                    string updateHA = $"UPDATE employee SET tickets = '{HAassigned}', closed_tickets = '{HAclosed}' WHERE employee_id = '{tickets.Employee}'";

                    if (connection.Execute(updateticket, tupdate) == 1 && connection.Execute(updateHA) == 1)
                    {
                        ViewData["Message"] = "Updated successfully.";
                    }
                    else
                    {
                        ViewData["Message"] = "Unsuccessful update. Do try again.";
                    }
                }

                //
                else if (tickets.Status == "closed" && tickets.Escalate_Reason != null)
                {
                    List<int> HAassignedticket = connection.Query<int>(assignedHAticket).AsList();
                    foreach (int id in HAassignedticket)
                    {
                        HAassigned = id - 1;
                    }

                    List<int> SEassignedticket = connection.Query<int>(assignedSEticket).AsList();
                    foreach (int id in SEassignedticket)
                    {
                        SEassigned = id - 1;
                    }

                    List<int> HAclosedticket = connection.Query<int>(closedHAticket).AsList();
                    foreach (int id in HAclosedticket)
                    {
                        HAclosed = id + 1;
                    }

                    List<int> SEclosedticket = connection.Query<int>(closedSEticket).AsList();
                    foreach (int id in SEclosedticket)
                    {
                        SEclosed = id + 1;
                    }

                    string updateHA = $"UPDATE employee SET tickets = '{HAassigned}', closed_tickets = '{HAclosed}' WHERE employee_id = '{tickets.Employee}'";

                    string updateSE = $"UPDATE employee e " +
                                      $"INNER JOIN ticket t ON t.employee_id = e.employee_id " +
                                      $"SET e.tickets = '{SEassigned}', e.closed_tickets = '{SEclosed}'  " +
                                      $"WHERE employee_id = '{tickets.Escalate_SE}'";

                    if (connection.Execute(updateticket, tupdate) == 1 && connection.Execute(updateHA) == 1 && connection.Execute(updateSE) == 1)
                    {
                        ViewData["Message"] = "Updated successfully.";
                    }
                    else
                    {
                        ViewData["Message"] = "Unsuccessful update. Do try again.";
                    }
                }
            }
            return RedirectToAction("ViewTicket", "Ticket");
        }

        public IActionResult SEResolution(int ticketid)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string tquery = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority,
                                         t.employee_id AS Employee, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution, t.escalation_SE AS Escalate_SE, t.escalate_reason
                                FROM ticket t 
                                INNER JOIN users u ON u.userid = t.userid 
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON t.employee_id = e.employee_id
                                WHERE t.ticket_id = @TicketId;";

                connection.Open();

                Ticket tickets = connection.QueryFirstOrDefault<Ticket>(tquery, new { TicketId = ticketid });

                if (tickets != null)
                {
                    return View(tickets);
                }
            }
            return View("ViewTicket");
        }

        [HttpPost]
        public IActionResult SEResolution(Ticket tickets)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                Ticket update = new Ticket()
                {
                    TicketId = tickets.TicketId,
                    newStatus = tickets.newStatus,
                    Resolution = tickets.Resolution,
                };

                string updateticket = @"UPDATE Ticket SET status = @newStatus, resolution = @Resolution WHERE ticket_id = @TicketId";

                if (connection.Execute(updateticket, update) == 1)
                {
                    ViewData["Message"] = "Updated successfully.";
                }
                else
                {
                    ViewData["Message"] = "Unsuccessful update. Do try again.";
                }
            }
            return RedirectToAction("ViewTicket", "Ticket");
        }

        public IActionResult ToDoTicket()
        {
            int? currentuser = contextAccessor.HttpContext.Session.GetInt32("userID");
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string equery = $"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority, t.employee_id, e.name AS EmployeeName, " +
                                    $"t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution " +
                                    $"FROM ticket t INNER JOIN users u ON u.userid = t.userid " +
                                    $"INNER JOIN ticket_categories tc ON tc.category_id = t.category_id " +
                                    $"INNER JOIN employee e ON t.employee_id = e.employee_id  " +
                                    $"WHERE t.employee_id='{currentuser}'";
                connection.Open();
                List<Ticket> tickets = connection.Query<Ticket>(equery).AsList();
                return View(tickets);
            }
        }

        public IActionResult DataCollected()
        {
            return View();
        }
    }
}
