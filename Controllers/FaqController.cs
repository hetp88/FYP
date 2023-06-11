using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using FYP.Models;
using System.Reflection.Metadata;

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

        public IActionResult CreateFAQ()
        {
            return View("CreateFAQ");
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "INSERT INTO FAQ (faq_id,category_id, question, solution) VALUES ('{0}','{1}', '{2}', '{3}')";
                SqlCommand command = new SqlCommand(query, connection);

                // Retrieve category ID based on the category name
                int categoryId = GetCategoryIdByName(faq.Category);
                if (categoryId == -1)
                {
                    // Category not found, handle accordingly (e.g., display an error message)
                    return false;
                }
                command.Parameters.AddWithValue("{0}",faq.FaqId);
                command.Parameters.AddWithValue("{1}", categoryId);
                command.Parameters.AddWithValue("{2}", faq.Question);
                command.Parameters.AddWithValue("{3}", faq.Solution);

                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0;
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

        [HttpPost]
        public IActionResult CreateFAQ(FAQ faq)
        {
            if (ModelState.IsValid)
            {
                bool isInserted = InsertFAQToDatabase(faq);
                if (isInserted)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {

                    
                }
            }
            return View(faq);
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
