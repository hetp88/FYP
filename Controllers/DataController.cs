using FYP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

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
        private string GetCurrentMonth()
        {
            string currentMonth = DateTime.Now.ToString("MMMM");
            return currentMonth;
        }
        private List<Ticket> GetTicketsFromDatabase()
        {
            List<Ticket> tickets = new List<Ticket>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                DateTime now = DateTime.Now;
                int currentMonth = now.Month;

                connection.Open();

                //Query to only take data from the current month we are in.
                string query = $"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, tc.category, t.status, t.datetime, t.priority, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution FROM ticket t INNER JOIN users u ON u.userid = t.userid INNER JOIN ticket_categories tc ON tc.category_id = t.category_id INNER JOIN employee e ON t.employee_id = e.employee_id WHERE MONTH(t.datetime)='{currentMonth}'";


                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Ticket ticket = new Ticket();
                            ticket.UserId = (int)reader["userid"];
                            ticket.Type = (string)reader["type"];
                            ticket.Description = (string)reader["description"];
                            ticket.Category = (string)reader["category"];
                            ticket.Status = (string)reader["status"];
                            ticket.DateTime = (DateTime)reader["datetime"];
                            ticket.Priority = (string)reader["priority"];
                            //Collecting the number of values for the diagrams
                            ticket.StatusCount = 0; 
                            ticket.PriorityCount = 0;
                            ticket.CategoryCount = 0;
                            ticket.TypeCount= 0;
                            tickets.Add(ticket); //Add the 
                        }
                    }
                }
            }
               //Adding up the counts for charts
            counts(tickets);

            return tickets;
        }

        private void counts(List<Ticket> tickets)
        {
            // Get the distinct statuses, priorities, and categories from the list of tickets
            List<string> distinctStatuses = tickets.Select(t => t.Status).Distinct().ToList();
            List<string> distinctPriorities = tickets.Select(t => t.Priority).Distinct().ToList();
            List<string> distinctCategories = tickets.Select(t => t.Category).Distinct().ToList();
            List<string> distinctTypes = tickets.Select(t => t.Type).Distinct().ToList();
            // Calculate and set the count for each status
            foreach (string status in distinctStatuses)
            {
                int count = tickets.Count(t => t.Status == status);
                foreach (Ticket ticket in tickets.Where(t => t.Status == status))
                {
                    ticket.StatusCount = count;
                }
            }

            // Calculate and set the count for each priority
            foreach (string priority in distinctPriorities)
            {
                int count = tickets.Count(t => t.Priority == priority);
                foreach (Ticket ticket in tickets.Where(t => t.Priority == priority))
                {
                    ticket.PriorityCount = count;
                }
            }

            // Calculate and set the count for each category
            foreach (string category in distinctCategories)
            {
                int count = tickets.Count(t => t.Category == category);
                foreach (Ticket ticket in tickets.Where(t => t.Category == category))
                {
                    ticket.CategoryCount = count;
                }
            }

            foreach (string types in distinctTypes)
            {
               int count = tickets.Count(t => t.Type == types);
                foreach (Ticket ticket in tickets.Where(t => t.Type == types))
                {
                    ticket.TypeCount = count;
                }
            }
        }
    }
}
