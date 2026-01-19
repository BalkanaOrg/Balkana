using Microsoft.AspNetCore.Mvc;

namespace Balkana.Controllers
{
    public class LegalController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult TOS()
        {
            return View();
        }
    }
}
