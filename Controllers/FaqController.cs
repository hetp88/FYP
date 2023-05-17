using Microsoft.AspNetCore.Mvc;

namespace FYP.Controllers
{
    public class FaqController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
