using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Players;
using Balkana.Data.Infrastructure;
using Balkana.Services.Players.Models;
using Microsoft.AspNetCore.Authorization;
using Balkana.Data.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Balkana.Services.Players;
using AutoMapper;
using Balkana.Services.Teams;
using Balkana.Models.Teams;

namespace Balkana.Controllers
{
    public class PlayersController : Controller
    {
        private readonly ApplicationDbContext data;
        private readonly IPlayerService players;
        private readonly IMapper mapper;

        public PlayersController(ApplicationDbContext data, IPlayerService players, IMapper mapper)
        {
            this.data = data;
            this.mapper = mapper;
            this.players = players;
        }


        public IActionResult Index([FromQuery] AllPlayersQueryModel query)
        {
            var queryResult = this.players.All(
                query.SearchTerm,
                query.CurrentPage,
                AllPlayersQueryModel.playersPerPage);

            var allNationalities = this.players.GetNationalities();

            query.TotalPlayers = queryResult.TotalPlayers;
            query.Players = queryResult.Players;

            return View(query);
        }


        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Add()
        {
            Console.WriteLine("Rendering Add player form.");
            return View(new PlayerFormModel
            {
                Nationalities = this.players.GetNationalities()
            });
        }

        [Authorize(Roles = "Administrator,Moderator")]
        [HttpPost]
        public IActionResult Add(PlayerFormModel player)
        {
            if (!this.data.Nationalities.Any(c=>c.Id == player.NationalityId))
            {
                this.ModelState.AddModelError(nameof(player.NationalityId), "This nationality isn't registered in the Database.");
            }

            if(ModelState.ErrorCount > 1)
            {
                player.Nationalities = this.players.GetNationalities();
                return View(player);
            }

            var playerData = this.players.Create(
                player.Nickname,
                player.FirstName,
                player.LastName,
                player.NationalityId);

            return RedirectToAction("Index", "Home");


            var pplayer = this.players.Profile(playerData);
            string playerInformation = pplayer.GetInformation();
            //TempData[GlobalMessageKey] = "This team has been added to the database.";

            return RedirectToAction(nameof(Profile), new { id = playerData, information = playerInformation });
        }

        public IActionResult Profile(int id, string information)
        {
            var player = this.players.Profile(id);

            if (player == null) return NotFound();

            if (information != player.GetInformation())
            {
                return BadRequest();
            }

            return View(player); // Bio page
        }

        public IActionResult Stats(int id, string? game = null)
        {
            var stats = this.players.Stats(id, game);

            if (stats == null) return NotFound();

            return View(stats); // Stats page
        }
    }
}
