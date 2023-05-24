using Microsoft.AspNetCore.Mvc;

namespace FYP.Controllers
{
    public class FaqController : Controller
    {
        public IActionResult Details()
        {
            return View();
        }
        public IActionResult CreateFAQ()
        {
            return View();
        }
        public IActionResult DeleteFAQ()
        {
           return View();
        }
    }
}
