using Microsoft.AspNetCore.Mvc;

namespace Balkana.Controllers
{
    public class StoreController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
