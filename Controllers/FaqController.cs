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

        private const string RECN = "Home";
        private const string REVW = "Index";
        public IActionResult Index()
        {
            return RedirectToAction("Details");
        }

        public IActionResult Details()
        {
            List<FAQ> faqs = GetFAQsFromDatabase();
            return View(faqs);
        }

        public IActionResult CreateFAQ(FAQ faq)
        {
            InsertFAQToDatabase(faq);

            // Redirect to the index homepage
            return RedirectToAction("Index", "Home");
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


        private bool InsertFAQToDatabase(FAQ faq)
        {
            {
                //string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"INSERT INTO FAQ (faq_id,category_id, question, solution) VALUES (@FaqId, @CategoryId, @Question, @Solution)";

                    connection.Open();
                    connection.ExecuteAsync(query, faq);
                }
                FAQ newFaq = new FAQ
                {
                    FaqId = new Random().Next(1, 1000001),
                    CategoryId = faq.CategoryId,
                    Question = faq.Question,
                    Solution = faq.Solution,
                };

                InsertFAQToDatabase(newFaq);

                return true;
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
