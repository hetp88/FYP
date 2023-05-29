using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using FYP.Models;

namespace FYP.Controllers
{
    public class FaqController : Controller
    {
        private readonly string _connectionString;

        public FaqController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            return RedirectToAction("Details");
        }

        public IActionResult Details()
        {
            List<FAQ> faqs = GetFAQsFromDatabase();
            return View(faqs);
        }

        public IActionResult CreateFAQ()
        {
            return View("CreateFAQ");
        }

        [HttpPost]
        public IActionResult CreateFAQ(FAQ faq)
        {
            if (ModelState.IsValid)
            {
                InsertFAQToDatabase(faq);
                return RedirectToAction("Details");
            }
            return View(faq);
        }

        [HttpPost]
        public IActionResult Delete(int faqId)
        {
            DeleteFAQFromDatabase(faqId);
            return RedirectToAction("Details");
        }

        private List<FAQ> GetFAQsFromDatabase()
        {
            List<FAQ> faqs = new List<FAQ>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT faq_id, category_id, question, solution FROM FAQ";
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    FAQ faq = new FAQ
                    {
                        FaqId = Convert.ToInt32(reader["faq_id"]),
                        CategoryId = Convert.ToInt32(reader["category_id"]),
                        Question = reader["question"].ToString(),
                        Solution = reader["solution"].ToString()
                    };
                    faqs.Add(faq);
                }
                reader.Close();
            }
            return faqs;
        }

        private void InsertFAQToDatabase(FAQ faq)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "INSERT INTO FAQ (faq_id, category_id, question, solution) VALUES (@FaqId, @CategoryId, @Question, @Solution)";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FaqId", faq.FaqId);
                command.Parameters.AddWithValue("@CategoryId", faq.CategoryId);
                command.Parameters.AddWithValue("@Question", faq.Question);
                command.Parameters.AddWithValue("@Solution", faq.Solution);
                connection.Open();
                command.ExecuteNonQuery();
            }
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
    }
}
