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
        private readonly string _connectionString;
        private readonly IHttpContextAccessor contextAccessor;

        public HomeController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            contextAccessor = httpContextAccessor;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection");
        }


        //[Authorize(Roles = "student, staff")]
        public IActionResult Index()
        {
            List<News> newsu = new List<News>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT MAX(news_id) AS highest_news_id
                                 FROM news;";

                connection.Open();
                newsu = connection.Query<News>(query).AsList();

            }
            return View(newsu);
        }
        public IActionResult Editor(News news)
        {
            int newsid = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string idQuery = @"SELECT MAX(faq_id) FROM FAQ";
                connection.Open();

                newsid = connection.QuerySingleOrDefault<int>(idQuery) + 1;


                News newUpdate = new News
                {
                    newsID = newsid + 1,
                    newsUpdate = news.newsUpdate,
                };

                string query = @"INSERT INTO news (news_id, news_u) VALUES (@newsID, @newsUpdate)";

                if (connection.Execute(query, newUpdate) == 1)
                {
                    TempData["Message"] = "Update announced successfully";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Message"] = "New Update published failed";
                    TempData["MsgType"] = "danger";
                }
            }
            // Redirect to the index homepage
            return RedirectToAction("Index", "Home");
        }
    }
}