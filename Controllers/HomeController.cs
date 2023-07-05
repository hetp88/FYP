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
        private readonly IHttpContextAccessor contextAccessor;

        public HomeController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            contextAccessor = httpContextAccessor;
        }
        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection");
        }

        
        //[Authorize(Roles = "student, staff")]
        public IActionResult Index()
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string query = @"SELECT news_id AS newsID , news_u AS newsU FROM News";

                connection.Open();
                List<News> newsu = connection.Query<News>(query).AsList();
                return View(newsu);
            }
        }
        public IActionResult Editor()
        {
            return View();
        }
        public IActionResult Editor(News news)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                string query = @"UPDATE News SET news_u = @NewsU WHERE news_id = @NewsID";

                connection.Open();
                connection.Execute(query, new { NewsU = news.newsU, NewsID = news.newsID });

                // Perform any additional logic or redirection as needed

                return RedirectToAction("Index");
            }
        }
    }
}