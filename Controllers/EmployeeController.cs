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
        public IActionResult ApplyLeave()
        {
            return View();
        }
        public IActionResult Schedule()
        {
            return View();
        }
        public IActionResult LeaveReqest()
        {
            return View();
        }
    }
}