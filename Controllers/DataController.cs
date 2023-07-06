using Microsoft.AspNetCore.Mvc;

namespace FYP.Controllers
{
    public class DataController : Controller
    {
        public IActionResult StatusData()
        {
            ViewData["Chart"] = "bar";
            ViewData["Title"] = "Completed Tickets Percentage (%)";
            ViewData["ShowLegend"] = "true";
            return View("DataCollected");
        }
        public IActionResult Priority()
        {
            return View("DataCollected");
        }
        public IActionResult Category()
        {
            return View("DataCollected");
        }
        public IActionResult DataCollected()
        {
            return View();
        }
    }
    
}
