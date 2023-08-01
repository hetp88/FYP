using FYP.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data.SqlTypes;


namespace FYP.Controllers
{
    public class DataController : Controller
    {
        private readonly string _connectionString;

        public DataController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        //=======================================Display the data between the 2 dates selected by User=================================================//
        public IActionResult DataCollected(DateTime startDate, DateTime endDate)
        {
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator"))
            {
                startDate = AdjustDate(startDate);
                endDate = AdjustDate(endDate);

                List<Ticket> tickets = GetTicketsFromDatabase(startDate, endDate);
                return View(tickets);
            }
            else
            {
                return View("Forbidden");
            }
            
        }
        //==========================================Solves the Date Issue (Between some acceptable dates by SQL)==============================================//
        private DateTime AdjustDate(DateTime date)
        {
            if (date < SqlDateTime.MinValue.Value)
                date = SqlDateTime.MinValue.Value;

            if (date > SqlDateTime.MaxValue.Value)
                date = SqlDateTime.MaxValue.Value;

            return date;
        }

        //=========================================Retrieve Values from the database===============================================//
        private List<Ticket> GetTicketsFromDatabase(DateTime startDate, DateTime endDate)
        {
            List<Ticket> tickets = new List<Ticket>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = $"SELECT t.ticket_id AS TicketId, t.userid, t.type, t.description, t.category_id, tc.category , t.status, t.datetime, t.priority, e.name AS EmployeeName, t.devices_involved AS DevicesInvolved, t.additional_details, t.resolution FROM ticket t INNER JOIN users u ON u.userid = t.userid INNER JOIN ticket_categories tc ON tc.category_id = t.category_id INNER JOIN employee e ON t.employee_id = e.employee_id WHERE t.datetime >= @StartDate AND t.datetime <= @EndDate";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    //Set the Start and end date to be retrieved
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Ticket ticket = new Ticket();
                            ticket.UserId = (int)reader["userid"];
                            ticket.Type = (string)reader["type"];
                            ticket.Description = (string)reader["description"];
                            ticket.category_id = (int)reader["category_id"];
                            ticket.Status = (string)reader["status"];
                            ticket.DateTime = (DateTime)reader["datetime"];
                            ticket.Priority = (string)reader["priority"];
                            ticket.EmployeeName = reader["EmployeeName"] == DBNull.Value ? null : (string)reader["EmployeeName"];                                                                        
                            ticket.StatusCount = 0;
                            ticket.PriorityCount = 0;
                            ticket.CategoryCount = 0;
                            ticket.TypeCount = 0;
                            tickets.Add(ticket); //Add the ticket to the list
                        }
                    }
                }
            }

            //Adding up the counts for charts
            counts(tickets);

            return tickets;
        }
        //========================================Counts to be added as data to be shown in diagram================================================//
        private void counts(List<Ticket> tickets)
        {
            List<string> distinctStatuses = tickets.Select(t => t.Status).Distinct().ToList();
            List<string> distinctPriorities = tickets.Select(t => t.Priority).Distinct().ToList();
            List<string> distinctCategories = tickets.Select(t => t.Category).Distinct().ToList();
            List<string> distinctTypes = tickets.Select(t => t.Type).Distinct().ToList();
            //Each Loop will add a value each time there is 1 and will add subsequently till none.
            foreach (string status in distinctStatuses)
            {
                int count = tickets.Count(t => t.Status == status); //Set the variable 
                foreach (Ticket ticket in tickets.Where(t => t.Status == status)) //Go through the loop where the title is "status" 
                {
                    ticket.StatusCount += 1; //For each count, add 1 to the overall value
                }
            }

            //Calculate and set the count for each priority
            foreach (string priority in distinctPriorities)
            {
                int count = tickets.Count(t => t.Priority == priority);
                foreach (Ticket ticket in tickets.Where(t => t.Priority == priority))
                {
                    ticket.PriorityCount += 1;
                }
            }
            // Calculate and set the count for each category
            foreach (string category in distinctCategories)
            {
                int count = tickets.Count(t => t.Category == category);
                foreach (Ticket ticket in tickets.Where(t => t.Category == category))
                {
                    ticket.CategoryCount += 1;
                }
            }
            foreach (string types in distinctTypes)
            {
               int count = tickets.Count(t => t.Type == types);
                foreach (Ticket ticket in tickets.Where(t => t.Type == types))
                {
                    ticket.TypeCount += 1;
                }
            }
        }
    }
}

//========================================Top 10 Employee Closed Tickets List (Not Working)================================================//
/*private List<News> GetEmployeeData()
{
    List<News> employees = new List<News>();

    using (SqlConnection connection = new SqlConnection(_connectionString))
    {
        connection.Open();
        string query = $"SELECT TOP 10 employee_id, name, email, phone_no, closed_tickets FROM employee WHERE MONTH(last_login) = MONTH(GETDATE()) ORDER BY closed_tickets DESC;";
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    News employee = new News
                    {
                        newsID = (int)reader["employee_id"],
                        newsU = (string)reader["name"],
                        closedT = (int)reader["closed_tickets"],
                        empname = (string)reader["name"]
                    };
                    employees.Add(employee);
                }
            }
        }
    }

    return employees;
}

public IActionResult EmployeeChart()
{
    List<News> employeeData = GetEmployeeData();
    return View(employeeData);
}*/
