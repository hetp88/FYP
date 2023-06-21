using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using FYP.Models;
using Dapper;

namespace FYP.Controllers
{
    public class TicketController : Controller
    {
        private readonly IConfiguration _configuration;

        public TicketController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult ViewTicket()
        {
            // Retrieve ticket data from the database
            List<Ticket> tickets = GetTicketData();

            return View(tickets);
        }

        private List<Ticket> GetTicketData()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT t.ticket_id, u.username, t.userid, t.description, tc.category, ts.status, 
                           t.datetime, tp.priority_type, e.name AS employee_name, t.devices_involved, 
                           t.additional_details
                    FROM ticket t
                    INNER JOIN users u ON u.userid = t.userid
                    INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                    INNER JOIN ticket_status ts ON ts.status_id = t.status_id
                    INNER JOIN ticket_priorities tp ON tp.priority_id = t.priority_id
                    INNER JOIN employee e ON e.employee_id = t.employee_id";

                connection.Open();
                List<Ticket> tickets = connection.Query<Ticket>(query).AsList();
                return tickets;
            }
        }
    }
}
