using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using FYP.Models;
using Dapper;
using System.Security.Cryptography;
using System;

namespace FYP.Controllers
{
    public class TicketController : Controller
    {
        private readonly string _connectionString;

        public TicketController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult ViewTicket()
        {
            // Retrieve ticket data from the database
            //List<Ticket> tickets = new List<Ticket>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, 
                                       t.datetime, t.priority, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution
                                FROM ticket t
                                INNER JOIN users u ON u.userid = t.userid
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON t.employee_id = e.employee_id;";

                connection.Open();
                List<Ticket> tickets = connection.Query<Ticket>(query).AsList();
                return View(tickets);
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
            int noticket = 0;
            DateTime created = DateTime.Now;
            string input = "";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string idQuery = @"SELECT MAX(ticket_id) FROM ticket";

                string empquery = @"SELECT e.employee_id 
                                    FROM employee 
                                    INNER JOIN l.leave ON l.employee_id = e.employee_id
                                    WHERE e.roles_id = 3 OR e.roles_id = 4
                                    AND e.startDate";

                connection.Open();

                List<int> tid = connection.Query<int>(idQuery).AsList();
                foreach (int id in tid)
                {
                    ticketid = id;
                }

                List<int> empid = connection.Query<int>(empquery).AsList();
                int emp = generate.Next(0, empid.Count);

                if (ticket.Additional_Details != null)
                {
                    input = ticket.Additional_Details;
                }
                else
                {
                    input = "-";
                }

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
                    Additional_Details = input,
                    Resolution = "-",
                };

                string empticket = $"SELECT tickets FROM employee WHERE employee_id = '{empid[emp]}'";
                List<int> eticket = connection.Query<int>(empticket).AsList();
                foreach (int id in eticket)
                {
                    noticket = id + 1;
                }

                Ticket empupdate = new Ticket
                {
                    Employee = empid[emp],
                };

                string query = @"INSERT INTO ticket (ticket_id, userid, type, description, category_id, status, datetime, priority, employee_id, devices_involved, additional_details, resolution) 
                                    VALUES (@TicketId, @UserId, @Type, @Description, @Category, @Status, @DateTime, @Priority, @Employee, @DevicesInvolved, @Additional_Details, @Resolution)";

                string update = $"UPDATE employee SET tickets = '{noticket}' WHERE employee_id = @Employee";

                if (connection.Execute(query, newTicket) == 1 && connection.Execute(update, empupdate) == 1)
                {
                    TempData["Message"] = "Ticket submitted successfully";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Message"] = "Ticket submit failed";
                    TempData["MsgType"] = "danger";
                }
            }
            return RedirectToAction("ViewTicket", "Ticket");
        }

        public IActionResult UpdateTicket()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UpdateTicket(int tid, Ticket ticket)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                Ticket Ticket = new Ticket
                {
                    Status = ticket.Status,
                    Resolution = ticket.Resolution,
                };

                string update = $"UPDATE INTO Ticket SET status = @Status, resolution = @Resolution WHERE ticket_id = {tid}";

                if (connection.Execute(update, Ticket) == 1)
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

        public IActionResult DataCollected()
        {
            return View();
        }
    }
}
