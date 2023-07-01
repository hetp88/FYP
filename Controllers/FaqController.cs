using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using FYP.Models;
using System.Reflection.Metadata;
using Dapper;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Linq;

namespace FYP.Controllers
{
    public class FaqController : Controller
    {
        private readonly string _connectionString;

        public FaqController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Details()
        {
            List<FAQ> faqs = new List<FAQ>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            { 
                string query = @"SELECT f.faq_id AS FaqId, tc.category, f.question, f.solution 
                                 FROM FAQ f
                                 INNER JOIN ticket_categories tc ON tc.category_id = f.category_id;";

                connection.Open();
                faqs = connection.Query<FAQ>(query).AsList();
                
            }
            return View(faqs);
        }
        
        public IActionResult CreateFAQ()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateFAQ(FAQ faq)
        {
            int faqid = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string idQuery = @"SELECT MAX(faq_id) FROM FAQ";
                connection.Open();

                faqid = connection.QuerySingleOrDefault<int>(idQuery) + 1;
                

                FAQ newFaq = new FAQ
                {
                    FaqId = faqid + 1,
                    Category = faq.Category,
                    Question = faq.Question,
                    Solution = faq.Solution,
                };

                string query = @"INSERT INTO FAQ (faq_id, category_id, question, solution) VALUES (@FaqId, @Category, @Question, @Solution)";
 
                if (connection.Execute(query, newFaq) == 1)
                {
                    TempData["Message"] = "FAQ published successfully";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Message"] = "FAQ published failed";
                    TempData["MsgType"] = "danger";
                }
            }
            // Redirect to the index homepage
            return RedirectToAction("Details", "FAQ");
        }

        public IActionResult Delete(int fid)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string delete = "DELETE FROM FAQ WHERE faq_id = {0}";

                connection.Open();
                
                if (connection.Execute(delete, fid) == 1)
                {
                    ViewData["Message"] = "Deleted successfully.";
                }
                else 
                {
                    ViewData["Message"] = "Unsuccessful delete. Do try again.";
                }
                return RedirectToAction("Details");
            }
        }        
    }
}
