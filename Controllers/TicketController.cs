using Microsoft.AspNetCore.Mvc;

namespace FYP.Controllers
{
    public class TicketController : Controller
    {
        public IActionResult ViewTicket()
        {
            return View();
        }
    }
}
