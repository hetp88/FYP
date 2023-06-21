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
            List<Ticket> tickets = new List<Ticket>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"SELECT t.ticket_id, t.userid, t.type, t.description, tc.category, t.status, 
                                       t.datetime, t.priority, e.name, t.devices_involved, t.additional_details, t.resolution
                                FROM ticket t
                                INNER JOIN users u ON u.userid = t.userid
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON t.employee_id = e.employee_id;";

                connection.Open();
                tickets = connection.Query<Ticket>(query).AsList();
                return View(tickets);
            }
        }
        public IActionResult AddTicket(Ticket ticket)
        {
            return View();
        }
        public IActionResult ToDoTicket()
        {
            List<Ticket> ticketed = GetTicketDataForEmp();
            return View();
        }

        public IActionResult DataCollected()
        {
            return View();
        }

        private List<Ticket> GetTicketDataForEmp() {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"SELECT t.ticket_id, t.userid, t.type, t.description, tc.category, t.status, 
       t.datetime, t.priority, e.name, t.devices_involved, t.additional_details, t.resolution
FROM ticket t
INNER JOIN users u ON u.userid = t.userid
INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
INNER JOIN employee e ON t.employee_id = e.employee_id
WHERE t.employee_id = {'0'};";
                    

                connection.Open();
                List<Ticket> ticketed = connection.Query<Ticket>(query).AsList();
                return ticketed;
            }
        }

        private List<Ticket> GetTicketData()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"SELECT t.ticket_id, t.userid, t.type, t.description, tc.category, t.status, 
                                       t.datetime, t.priority, e.name, t.devices_involved, t.additional_details, t.resolution
                                FROM ticket t
                                INNER JOIN users u ON u.userid = t.userid
                                INNER JOIN ticket_categories tc ON tc.category_id = t.category_id
                                INNER JOIN employee e ON t.employee_id = e.employee_id;";

                connection.Open();
                List<Ticket> tickets = connection.Query<Ticket>(query).AsList();
                return tickets;
            }
        }
    }
}
