using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Maps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Balkana.Controllers
{
    public class MapsController : Controller
    {
        private readonly ApplicationDbContext data;

        public MapsController(ApplicationDbContext data)
            => this.data = data;

        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Add()
        {
            return View(new AddMapFormModel());
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Add(AddMapFormModel map)
        {
            if(ModelState.ErrorCount > 0)
            {
                return View(map);
            }
            var mapData = new GameMap
            {
                Name = map.Name,
                PictureURL = map.PictureURL,
                isActiveDuty = map.isActiveDuty
            };

            this.data.GameMaps.Add(mapData);
            this.data.SaveChanges();

            return RedirectToAction("Index", "Home");
        }
    }
}
