using Microsoft.AspNetCore.Mvc;

namespace FYP.Controllers
{
    public class TicketController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
