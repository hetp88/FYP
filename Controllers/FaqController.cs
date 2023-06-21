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
            List<FAQ> faqs = GetFAQsFromDatabase();
            return View(faqs);
        }
        private List<FAQ> GetFAQsFromDatabase()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT f.faq_id, tc.category, f.question, f.solution 
                                 FROM FAQ f
                                 INNER JOIN ticket_categories tc ON tc.category_id = f.category_id;";

                connection.Open();
                List<FAQ> faqs = connection.Query<FAQ>(query).AsList();
                return faqs;
            }
        }
        public IActionResult CreateFAQ()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateFAQ(FAQ faq)
        {
            int categoryid = 0;
            int faqid = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string idQuery = $"SELECT faq_id FROM FAQ";
                string catQuery = $"SELECT tc.category_id FROM ticket_categories tc INNER JOIN FAQ f ON f.category_id = tc.category_id WHERE tc.category = '{faq.Category}';";
                
                connection.Open();

                List<int> cat = connection.Query<int>(catQuery).AsList();
                foreach (int cid in cat)
                {
                    categoryid = id;
                }

                List<int> faqs = connection.Query<int>(idQuery).AsList();
                foreach (int fid in faqs)
                {
                    faqid = fid;
                }

                FAQ newFaq = new FAQ
                {
                    FaqId = faqid + 1,
                    CategoryId = categoryid,
                    Question = faq.Question,
                    Solution = faq.Solution,
                };

                string query = @"INSERT INTO FAQ (faq_id, category_id, question, solution) VALUES (@FaqId, @CategoryId, @Question, @Solution)";
 
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
        private bool InsertFAQToDatabase(FAQ faq)
        {
            {
                //string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"INSERT INTO FAQ (faq_id, category_id, question, solution) VALUES (@FaqId, @CategoryId, @Question, @Solution)";

                    connection.Open();
                    connection.Execute(query, faq);
                }
                return true;
            }
        }

        [HttpPost]
        public IActionResult Delete(int faqId)
        {
            DeleteFAQFromDatabase(faqId);
            return RedirectToAction("Details");
        }
        private void DeleteFAQFromDatabase(int faqId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM FAQ WHERE faq_id = @FaqId";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FaqId", faqId);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        
        private int GetCategoryIdByName(string categoryName)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT category_id FROM ticket_categories WHERE category = @Category";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@category", categoryName);

                connection.Open();
                object result = command.ExecuteScalar();
                int categoryId = result != null ? Convert.ToInt32(result) : -1;

                return categoryId;
            }
        }
        
    }
}
