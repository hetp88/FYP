﻿using System;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace FYP.Controllers
{
    public class FaqController : Controller
    {
        private readonly string _connectionString;

        public FaqController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        //========================================FAQ Details================================================//
        public IActionResult Details(string faqIdQuery, string questionQuery, string solutionQuery, string categoryQuery)
        {
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator") || User.IsInRole("student") || User.IsInRole("staff"))
            {
                List<FAQ> faqs = new List<FAQ>();
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string selectQuery = @"SELECT f.faq_id AS FaqId, tc.category, f.question, f.solution 
                               FROM FAQ f
                               INNER JOIN ticket_categories tc ON tc.category_id = f.category_id
                               WHERE (@FaqIdQuery IS NULL OR f.faq_id = @FaqIdQuery)
                                   AND (@QuestionQuery IS NULL OR f.question LIKE '%' + @QuestionQuery + '%')
                                   AND (@SolutionQuery IS NULL OR f.solution LIKE '%' + @SolutionQuery + '%')
                                   AND (@CategoryQuery IS NULL OR tc.category_id = @CategoryQuery)
                                   AND f.faq_id > 0";


                    int categoryId = 0;
                    if (!string.IsNullOrEmpty(categoryQuery))
                    {
                        string categoryIdQuery = "SELECT category_id FROM ticket_categories WHERE category = @Category";
                        categoryId = connection.QuerySingleOrDefault<int>(categoryIdQuery, new { Category = categoryQuery });
                    }

                    connection.Open();
                    faqs = connection.Query<FAQ>(selectQuery, new
                    {
                        FaqIdQuery = string.IsNullOrEmpty(faqIdQuery) ? null : faqIdQuery,
                        QuestionQuery = string.IsNullOrEmpty(questionQuery) ? null : questionQuery,
                        SolutionQuery = string.IsNullOrEmpty(solutionQuery) ? null : solutionQuery,
                        CategoryQuery = string.IsNullOrEmpty(categoryQuery) ? null : categoryQuery
                    }).AsList();
                }

                return View(faqs);
            }
            else
            {
                return View("Forbidden");
            }

        }
        //========================================Solution Extention of details================================================//

        public IActionResult Solution(int FaqId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string q = @"SELECT f.faq_id AS FaqId, tc.category, f.question, f.solution 
                               FROM FAQ f
                               INNER JOIN ticket_categories tc ON tc.category_id = f.category_id
                               WHERE (f.faq_id = @FaqId)"
                ;
                connection.Open();

                FAQ Details = connection.QueryFirstOrDefault<FAQ>(q, new { FaqId = FaqId });
                if (Details != null)
                {
                    return View(Details);
                }
            }
            return RedirectToAction("Details");
        }
        //==========================================Search Function==============================================//

        public IActionResult Search(string query)
        {
            List<FAQ> faqs = new List<FAQ>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string searchQuery = @"SELECT f.faq_id AS FaqId, tc.category, f.question, f.solution 
                               FROM FAQ f
                               INNER JOIN ticket_categories tc ON tc.category_id = f.category_id
                               WHERE f.question LIKE '%' + @Query + '%';";

                connection.Open();
                faqs = connection.Query<FAQ>(searchQuery, new { Query = query }).AsList();
            }

            return View("Details", faqs);
        }

        //===========================================Create New FAQ=============================================//

        public IActionResult CreateFAQ()
        {
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator"))
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
        public IActionResult CreateFAQ(FAQ faq)
        {
            if (User.IsInRole("helpdesk agent") || User.IsInRole("support engineer") || User.IsInRole("administrator"))
            {
                int faqid = 0;
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string idQuery = @"SELECT MAX(faq_id) FROM FAQ";
                    connection.Open();

                    var maxId = connection.QuerySingleOrDefault<int?>(idQuery);

                    List<int> tid = connection.Query<int>(idQuery).AsList();
                    foreach (int id in tid)
                    {
                        faqid = id;
                    }

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
            else
            {
                // Unauthorized actions for other roles
                return View("Forbidden");
            }

        }

        //===========================================Delete FAQ=============================================//

        public IActionResult Delete(int faqId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Delete the FAQ with the specified ID
                        string deleteQuery = "DELETE FROM FAQ WHERE faq_id = @FaqId";
                        connection.Execute(deleteQuery, new { FaqId = faqId }, transaction);

                        transaction.Commit();

                        // Redirect to the updated FAQ list
                        return RedirectToAction("Details");
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}