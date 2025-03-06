using Microsoft.AspNetCore.Mvc;

namespace Balkana.Controllers
{
    public class TransfersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
