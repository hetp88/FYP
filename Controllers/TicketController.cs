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
                string query = @"SELECT t.ticket_id, t.userid, t.type, t.description, tc.category, t.status, 
                                       t.datetime, t.priority, e.name, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution
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
            int uid = 0;
            //int? currentuser = contextAccessor.HttpContext.Session.GetInt32("userID");
            var generate = new Random();
            DateTime now = DateTime.Now;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string idQuery = $"SELECT MAX(ticket_id) FROM ticket";

                //string userquery = $"SELECT userid FROM users WHERE username='{currentuser}'";

                string empquery = $"SELECT employee_id FROM employee";

                connection.Open();

                List<int> tid = connection.Query<int>(idQuery).AsList();
                foreach (int id in tid)
                {
                    ticketid = id;
                }

                //List<int> userid = connection.Query<int>(userquery).AsList();
                //foreach (int id in userid)
                //{
                //uid = id;
                //Console.WriteLine(uid);
                //}
                //Console.WriteLine(uid);

                List<int> empid = connection.Query<int>(empquery).AsList();

                Ticket newTicket = new Ticket
                {
                    TicketId = ticketid + 1,
                    UserId = uid,
                    Type = ticket.Type,
                    Description = ticket.Description,
                    Category = ticket.Category,
                    Status = "new",
                    DateTime = Convert.ToDateTime(now),
                    Priority = ticket.Priority,
                    Employee = generate.Next(empid.Count),
                    DevicesInvolved = ticket.DevicesInvolved,
                    Additional_Details = ticket.Additional_Details,
                    Resolution = "null",
                };

                string query = @"INSERT INTO ticket (ticket_id, userid, type, description, category_id, status, datetime, priority, employee_id, devices_involved, additional_details, resolution) 
                                    VALUES (@TicketId, @UserId, @Type, @Description, @Category, @Status, @DateTime, @Priority, @Employee, @DevicesInvolved, @Additional_Details, @Resolution)";

                if (connection.Execute(query, newTicket) == 1)
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
            return RedirectToAction("Ticket", "ViewTicket");
        }

        public IActionResult DataCollected()
        {
            return View();
        }
    }
}
