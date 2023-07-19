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
using XAct.Users;

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
            if ( User.IsInRole("student") || User.IsInRole("staff"))
            {
                //GET CURRENT USER ID
                int? currentuser = contextAccessor.HttpContext.Session.GetInt32("userID");

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = $"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority, e.name AS EmployeeName, " +
                                         $"t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution " +
                                   $"FROM ticket t INNER JOIN users u ON u.userid = t.userid " +
                                   $"INNER JOIN ticket_categories tc ON tc.category_id = t.category_id " +
                                   $"INNER JOIN employee e ON e.employee_id = t.employee_id  " +
                                   $"WHERE t.userid='{currentuser}'";

                    connection.Open();
                    List<Ticket> tickets = connection.Query<Ticket>(query).AsList();
                    return View(tickets);
                }
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
            
        }

        public IActionResult ToDoTicket()
        {
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer"))
            {
                int? currentuser = contextAccessor.HttpContext.Session.GetInt32("userID");

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string equery = $"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority, t.employee_id, e.name AS EmployeeName, " +
                                         $"t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution, t.escalation_SE AS Escalate_SE, t.escalate_reason " +
                                    $"FROM ticket t " +
                                    $"INNER JOIN users u ON u.userid = t.userid " +
                                    $"INNER JOIN ticket_categories tc ON tc.category_id = t.category_id " +
                                    $"INNER JOIN employee e ON e.employee_id = t.employee_id  " +
                                    $"WHERE t.employee_id='{currentuser}'" +
                                    $"OR t.escalation_SE = '{currentuser}'";
                   
                    connection.Open();
                    List<Ticket> tickets = connection.Query<Ticket>(equery).AsList();
                    
                    return View(tickets);
                }
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
        }

        public IActionResult ViewTicket()
        {
            if (User.IsInRole("administrator"))
            {
                // Retrieve ticket data from the database

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, 
                                       t.datetime, t.priority, e.name AS EmployeeName, t.employee_id AS Employee, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution, t.escalation_SE AS Escalate_SE, t.escalate_reason
                                FROM ticket t
                                INNER JOIN users u ON u.userid = t.userid
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON e.employee_id = t.employee_id;";

                    connection.Open();
                    List<Ticket> tickets = connection.Query<Ticket>(query).AsList();

                    return View(tickets);
                }
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
        }

        public IActionResult SearchTicket(string ticketIdQuery, string userIdQuery, string ticketTypeQuery, string statusQuery, string priorityQuery, string employeeQuery)
        {
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator") || User.IsInRole("student") || User.IsInRole("staff"))
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
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
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
            int? userid = 0;
            Random generate = new Random();
            int assignedticket = 0;
            DateTime created = DateTime.Now;
            string user_email = "";
            string user_name = "";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string idQuery = @"SELECT MAX(ticket_id) FROM ticket";

                string empquery = @"SELECT DISTINCT e.employee_id
                                    FROM employee e
                                    LEFT JOIN leave l ON l.employee_id = e.employee_id
                                    WHERE e.roles_id = 3
                                    AND (e.tickets IS NULL OR e.tickets < 5)
                                    AND ((GETDATE() NOT BETWEEN l.startDate AND l.end_date)
                                        OR (GETDATE() <> l.startDate AND GETDATE() <> l.end_date) 
                                    AND (l.is_approved = 'Rejected' OR l.is_approved IS NULL)
                                        OR l.employee_id IS NULL)";

                connection.Open();

                List<int> tid = connection.Query<int>(idQuery).AsList();
                foreach (int id in tid)
                {
                    ticketid = id;
                }

                List<int> empid = connection.Query<int>(empquery).AsList();
                int emp = generate.Next(0, empid.Count);

                if(User.IsInRole("helpdesk agent"))
                {
                    userid = ticket.UserId;
                }
                else if (User.IsInRole("staff") || User.IsInRole("student"))
                {
                    userid = contextAccessor.HttpContext.Session.GetInt32("userID");
                }

                Ticket newTicket = new Ticket
                {
                    TicketId = ticketid + 1,
                    UserId = userid,
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
                    Escalate_SE = 0,
                };

                int eid = empid[emp];

                string empticket = $"SELECT tickets FROM employee WHERE employee_id = '{eid}'";

                List<int> eticket = connection.Query<int>(empticket).AsList();
                foreach (int id in eticket)
                {
                    assignedticket = id + 1;
                }

                string query = @"INSERT INTO ticket (ticket_id, userid, type, description, category_id, status, datetime, priority, employee_id, devices_involved, additional_details, resolution, escalate_reason, escalation_SE) 
                                VALUES (@TicketId, @UserId, @Type, @Description, @Category, @Status, @DateTime, @Priority, @Employee, @DevicesInvolved, @Additional_Details, @Resolution, @Escalate_Reason, @Escalate_SE)";

                string update = $"UPDATE employee SET tickets = '{assignedticket}' WHERE employee_id = '{eid}'";

                string getUserinfo = $"SELECT u.username, u.email " +
                                        $"FROM users u " +
                                        $"INNER JOIN ticket t ON t.userid = u.userid " +
                                        $"WHERE u.userid = '{newTicket.UserId}'";

                List<Ticket> userinfo = connection.Query<Ticket>(getUserinfo).AsList();
                foreach (Ticket info in userinfo)
                {
                    user_name = info.Username;
                    user_email = info.Email;
                }

                if (connection.Execute(query, newTicket) == 1 && connection.Execute(update) == 1)
                {
                    string link = "https://localhost:44397/Account/Login";
                    string delete = "https://localhost:44397/Ticket/Terminate/" + newTicket.TicketId;

                    string template = "Hello {0}, " +
                                        "<br>" +
                                        "<br>We have received your {1} ticket. " +
                                        "<br>" +
                                        "<br>Ticket id: <b>{2}</b>" +
                                        "<br>Description of ticket is <b>{3}</b>. " +
                                        "<br>" +
                                        "<br>Status of ticket can be checked when you <a href =\"" + link + "\">login</a>" +
                                        "<br>We will get back you as soon as possible. " +
                                        "<br>" +
                                        "<br>Thank you, " +
                                        "<br>RP IT HelpDesk";

                    string title = "Ticket Submitted Successful";

                    string message = String.Format(template, user_name, newTicket.Type, newTicket.TicketId, newTicket.Description);

                    if (EmailUtl.SendEmail(user_email, title, message, out string result))
                    {
                        ViewData["Message"] = "Ticket submitted successfully";
                        ViewData["MsgType"] = "success";
                    }
                    else
                    {
                        ViewData["Message"] = result;
                        ViewData["MsgType"] = "warning";
                    }
                }
                else
                {
                    TempData["Message"] = "Ticket submit failed";
                    TempData["MsgType"] = "danger";
                }
            }
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator"))
            {
                return RedirectToAction("ToDoTicket", "Ticket");
            }
            else if (User.IsInRole("student") || User.IsInRole("staff"))
            {
                return RedirectToAction("UserTicket", "Ticket");
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
            
        }

        public IActionResult EscalateTicket(int ticketid)
        {
            if (User.IsInRole("helpdesk agent"))
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string tquery = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority,
                                             t.employee_id AS Employee, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution, t.escalation_SE AS Escalate_SE, t.escalate_reason
                                    FROM ticket t 
                                    INNER JOIN users u ON u.userid = t.userid 
                                    INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                    INNER JOIN employee e ON e.employee_id = t.employee_id
                                    WHERE t.ticket_id = @TicketId;";

                    connection.Open();

                    //Ticket leaveRequest = connection.QueryFirstOrDefault<Ticekt>(query, new { LeaveId = leaveId });

                    Ticket tickets = connection.QueryFirstOrDefault<Ticket>(tquery, new { TicketId = ticketid });

                    if (tickets != null)
                    {
                        return View(tickets);
                    }
                }
                return View("ToDoTicket");
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
        }

        [HttpPost]
        public IActionResult EscalateTicket(Ticket ticket)
        {
            if (User.IsInRole("helpdesk agent"))
            {
                Random generate = new Random();
                int assignedticket = 0;

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string equery = @"SELECT DISTINCT e.employee_id
                                        FROM employee e
                                        LEFT JOIN leave l ON l.employee_id = e.employee_id
                                        WHERE e.roles_id = 4
                                        AND (e.tickets IS NULL OR e.tickets < 5)
                                        AND ((GETDATE() NOT BETWEEN l.startDate AND l.end_date)
  	                                        OR (GETDATE() <> l.startDate AND GETDATE() <> l.end_date) 
                                        AND (l.is_approved = 'Rejected' OR l.is_approved IS NULL)
                                            OR l.employee_id IS NULL)";

                    List<int> emid = connection.Query<int>(equery).AsList();
                    int emp = generate.Next(0, emid.Count);

                    connection.Open();

                    Ticket escalation = new Ticket
                    {
                        TicketId = ticket.TicketId,
                        Status = "waiting for resolution",
                        Employee = emid[emp],
                        Escalate_Reason = ticket.Escalate_Reason,
                    };

                    string update = @"UPDATE Ticket SET status = @Status, escalation_SE = @Employee, escalate_reason = @Escalate_Reason WHERE ticket_id = @TicketId";

                    string empticket = $"SELECT tickets FROM employee WHERE employee_id = '{emid[emp]}'";

                    List<int> eticket = connection.Query<int>(empticket).AsList();

                    foreach (int id in eticket)
                    {
                        assignedticket = id + 1;
                    }

                    string assign = $"UPDATE employee SET tickets = '{assignedticket}' WHERE employee_id = '{emid[emp]}'";

                    if (connection.Execute(update, escalation) == 1 && connection.Execute(assign) == 1)
                    {
                        ViewData["Message"] = "Escalated successfully.";
                    }
                    else
                    {
                        ViewData["Message"] = "Unsuccessful escalate. Do try again.";
                    }
                }
                return View("ToDoTicket");
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
        }

        public IActionResult HAUpdateTicket(int ticketid)
        {
            if (User.IsInRole("helpdesk agent"))
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string tquery = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority,
                                         t.employee_id AS Employee, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution, t.escalation_SE AS Escalate_SE, t.escalate_reason
                                FROM ticket t 
                                INNER JOIN users u ON u.userid = t.userid 
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON e.employee_id = t.employee_id
                                WHERE t.ticket_id = @TicketId;";

                    connection.Open();

                    Ticket tickets = connection.QueryFirstOrDefault<Ticket>(tquery, new { TicketId = ticketid });

                    if (tickets != null)
                    {
                        return View(tickets);
                    }
                }
                return View("ToDoTicket");
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
            
        }

        [HttpPost]
        public IActionResult HAUpdateTicket(Ticket tickets)
        {
            if (User.IsInRole("helpdesk agent"))
            {
                int HAassigned = 0;
                int SEassigned = 0;
                int HAclosed = 0;
                int SEclosed = 0;

                string updateticket = "";
                string user_name = "";
                string user_email = "";

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    Ticket tupdate = new Ticket()
                    {
                        TicketId = tickets.TicketId,
                        UserId = tickets.UserId,
                        Employee = tickets.Employee,
                        Description = tickets.Description,
                        Escalate_SE = tickets.Escalate_SE,
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
                    string assignedHAticket = $"SELECT tickets FROM employee WHERE employee_id = '{tupdate.Employee}'";

                    //closedHAticket --> to add 1 from current number of tickets closed for helpdesk agent if status is updated to 'closed'
                    string closedHAticket = $"SELECT closed_tickets FROM employee WHERE employee_id = '{tupdate.Employee}'";

                    //assignedSEticket --> to minus of 1 from current number of tickets escalated/assigned for support engineer if status is updated to 'closed'
                    string assignedSEticket = $"SELECT e.tickets " +
                                              $"FROM employee e " +
                                              $"INNER JOIN ticket t ON t.escalation_SE = e.employee_id " +
                                              $"WHERE e.employee_id = '{tupdate.Escalate_SE}'";

                    //closedSEticket --> to add 1 from current number of tickets closed for support engineer if status is updated to 'closed'
                    string closedSEticket = $"SELECT e.closed_tickets " +
                                            $"FROM employee e " +
                                            $"INNER JOIN ticket t ON t.escalation_SE = e.employee_id " +
                                            $"WHERE e.employee_id = '{tupdate.Escalate_SE}'";

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

                    List<int> SEassignedticket = connection.Query<int>(assignedSEticket).AsList();
                    foreach (int id in SEassignedticket)
                    {
                        SEassigned = id - 1;
                    }

                    List<int> SEclosedticket = connection.Query<int>(closedSEticket).AsList();
                    foreach (int id in SEclosedticket)
                    {
                        SEclosed = id + 1;
                    }

                    string updateHA = $"UPDATE employee SET tickets = '{HAassigned}', closed_tickets = '{HAclosed}' WHERE employee_id = '{tupdate.Employee}'";

                    string getUserinfo = $"SELECT u.username, u.email " +
                                         $"FROM users u " +
                                         $"INNER JOIN ticket t ON t.userid = u.userid " +
                                         $"WHERE u.userid = '{tupdate.UserId}'";

                    List<Ticket> userinfo = connection.Query<Ticket>(getUserinfo).AsList();
                    foreach (Ticket info in userinfo)
                    {
                        user_name = info.Username;
                        user_email = info.Email;
                    }

                    //
                    if (tupdate.newStatus == "closed" && tupdate.Escalate_SE == 0)
                    {

                        if (connection.Execute(updateticket, tupdate) == 1 && connection.Execute(updateHA) == 1)
                        {
                            string link = "https://localhost:44397/Account/Login";

                            string template = "Hello {0}, " +
                                              "<br>" +
                                              "<br>We have closed your {1} ticket. " +
                                              "<br>" +
                                              "<br>Ticket id: <b>{2}</b>" +
                                              "<br>Description of ticket is <b>{3}</b>. " +
                                              "<br>" +
                                              "<br>Resolution of ticket can be checked when you <a href =\"" + link + "\">login</a>" +
                                              "<br>Thank you for your patience. " +
                                              "<br>" +
                                              "<br>Regards, " +
                                              "<br>RP IT HelpDesk";

                            string title = "Ticket Closed";

                            string message = String.Format(template, user_name, tupdate.Type, tupdate.TicketId, tupdate.Description);

                            if (EmailUtl.SendEmail(user_email, title, message, out string result))
                            {
                                ViewData["Message"] = "Updated successfully.";
                            }
                            else
                            {
                                ViewData["Message"] = result;
                                ViewData["MsgType"] = "warning";
                            }
                        }
                        else
                        {
                            ViewData["Message"] = "Unsuccessful update. Do try again.";
                        }
                    }

                    //
                    else if (tupdate.newStatus == "closed" && tupdate.Escalate_SE > 0)
                    {
                        string updateSE = $"UPDATE e " +
                                          $"SET e.tickets = '{SEassigned}', e.closed_tickets = '{SEclosed}' " +
                                          $"FROM employee e " +
                                          $"INNER JOIN ticket t ON t.escalation_SE = e.employee_id " +
                                          $"WHERE e.employee_id = '{tupdate.Escalate_SE}'";

                        if (connection.Execute(updateticket, tupdate) == 1 && connection.Execute(updateHA) == 1 && connection.Execute(updateSE) == 1)
                        {
                            string link = "https://localhost:44397/Account/Login";

                            string template = "Hello {0}, " +
                                              "<br>" +
                                              "<br>We have closed your {1} ticket. " +
                                              "<br>" +
                                              "<br>Ticket id: <b>{2}</b>" +
                                              "<br>Description of ticket is <b>{3}</b>. " +
                                              "<br>" +
                                              "<br>Resolution of ticket can be checked when you <a href =\"" + link + "\">login</a>" +
                                              "<br>Thank you for your patience. " +
                                              "<br>" +
                                              "<br>Regards, " +
                                              "<br>RP IT HelpDesk";

                            string title = "Ticket Closed";

                            string message = String.Format(template, user_name, tupdate.Type, tupdate.TicketId, tupdate.Description);

                            if (EmailUtl.SendEmail(user_email, title, message, out string result))
                            {
                                ViewData["Message"] = "Updated successfully.";
                            }
                            else
                            {
                                ViewData["Message"] = result;
                                ViewData["MsgType"] = "warning";
                            }
                        }
                        else
                        {
                            ViewData["Message"] = "Unsuccessful update. Do try again.";
                        }
                    }

                    else if (tickets.Status != "closed")
                    {
                        if (connection.Execute(updateticket, tupdate) == 1)
                        {
                            ViewData["Message"] = "Updated successfully.";
                        }
                        else
                        {
                            ViewData["Message"] = "Unsuccessful update. Do try again.";
                        }
                    }
                }
                return RedirectToAction("ToDoTicket", "Ticket");
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
        }

        public IActionResult SEResolution(int ticketid)
        {
            if (User.IsInRole("support engineer"))
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string tquery = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority,
                                         t.employee_id AS Employee, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution, t.escalation_SE AS Escalate_SE, t.escalate_reason
                                FROM ticket t 
                                INNER JOIN users u ON u.userid = t.userid 
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON e.employee_id = t.employee_id
                                WHERE t.ticket_id = @TicketId;";

                    connection.Open();

                    Ticket tickets = connection.QueryFirstOrDefault<Ticket>(tquery, new { TicketId = ticketid });

                    if (tickets != null)
                    {
                        return View(tickets);
                    }
                }
                return View("ToDoTicket");
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }  
        }

        [HttpPost]
        public IActionResult SEResolution(Ticket tickets)
        {
            if (User.IsInRole("support engineer"))
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
                return View("ToDoTicket");
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
        }

    }
}
