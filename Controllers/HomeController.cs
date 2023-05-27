using FYP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;


namespace FYP.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //[Authorize(Roles = "student, staff")]
        public IActionResult Index()
        {
            List<Ticket> userTickets = GetTickets();

            return View(userTickets);
        }

        private List<Ticket> GetTickets()
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
                    INNER JOIN employee e ON e.employee_id = t.employee_id
                    WHERE t.userid = u.userid";

                connection.Open();
                List<Ticket> userTickets = connection.Query<Ticket>(query).AsList();
                return userTickets;
            }
        }
    }
}