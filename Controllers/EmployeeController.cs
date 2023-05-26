using FYP.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FYP.Controllers
{
    public class EmployeeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}