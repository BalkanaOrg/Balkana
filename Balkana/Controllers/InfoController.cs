using Microsoft.AspNetCore.Mvc;

namespace Balkana.Controllers
{
    public class InfoController : Controller
    {
        public IActionResult Balkana()
        {
            return View();
        }
        public IActionResult ContentCreators()
        {
            return View();
        }
        public IActionResult Tournaments()
        {
            return View();
        }
    }
}
