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
        [HttpPost]
        public IActionResult Editor(News news)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();
                News News = new News
                {
                    newsU = news.newsU,
                };
                string queryU = $"UPDATE News SET news_u = @newsU WHERE news_id = 1";
                if (connection.Execute(queryU, news) == 1)
                {
                    ViewData["Message"] = "Updated successfully.";
                }
                else
                {
                    ViewData["Message"] = "Unsuccessful update. Do try again.";
                }
            }
            // Perform any additional logic or redirection as needed

            return RedirectToAction("Index");
        }
    }
}