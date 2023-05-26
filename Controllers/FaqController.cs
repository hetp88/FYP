using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FYP.Controllers
{
    public class FaqController : Controller
    {
        public IActionResult Details()
        {
            return View();
        }
        [Authorize(Roles = "admin, employee")]
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
