using FYP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Collections.Generic;


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

        
 
        public IActionResult Index()
        {
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator") || User.IsInRole("student") || User.IsInRole("staff")) 
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    string query = @"SELECT news_id AS newsID , news_u AS newsU FROM News";

                    connection.Open();
                    List<News> newsu = connection.Query<News>(query).AsList();
                    return View(newsu);
                }
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
            
        }
        public IActionResult Editor()
        {
            if (User.IsInRole("administrator") )
            {
                return View();
            }
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }
        }
        [HttpPost]
        public IActionResult Editor(News news)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {

                string queryU = $"UPDATE News SET news_u = @newsU WHERE news_id = 1";
                connection.Open();
                connection.Execute(queryU, news);
            }
            return RedirectToAction("Index");
        }
    }
}
