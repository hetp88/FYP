using FYP.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace FYP.Controllers
{
    public class DataController : Controller
    {
        private readonly string _connectionString;

        public DataController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult DataCollected()
        {
            List<Ticket> tickets = GetTicketsFromDatabase();

            return View(tickets);
        }

        private List<Ticket> GetTicketsFromDatabase()
        {
            List<Ticket> tickets = new List<Ticket>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, 
                                       t.datetime, t.priority, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution
                                FROM ticket t
                                INNER JOIN users u ON u.userid = t.userid
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON t.employee_id = e.employee_id;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Ticket ticket = new Ticket();
                            ticket.UserId = (int)reader["userid"];
                            ticket.Description = (string)reader["description"];
                            ticket.Category = (string)reader["category"];
                            ticket.Status = (string)reader["status"];
                            ticket.DateTime = (DateTime)reader["datetime"];
                            ticket.Priority = (string)reader["priority"];
                            tickets.Add(ticket);
                        }
                    }
                }
            }

            return tickets;
        }
    }
}
