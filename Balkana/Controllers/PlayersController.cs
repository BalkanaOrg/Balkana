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
using Balkana.Services.Images;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Balkana.Controllers
{
    public class PlayersController : Controller
    {
        private readonly ApplicationDbContext data;
        private readonly IPlayerService players;
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment env;

        public PlayersController(ApplicationDbContext data, IPlayerService players, IMapper mapper, IWebHostEnvironment env)
        {
            this.data = data;
            this.mapper = mapper;
            this.players = players;
            this.env = env;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(PlayerFormModel player)
        {
            player.Id = 0;

            if (!this.data.Nationalities.Any(c=>c.Id == player.NationalityId))
            {
                this.ModelState.AddModelError(nameof(player.NationalityId), "This nationality isn't registered in the Database.");
            }

            if (!ModelState.IsValid)
            {
                player.Nationalities = this.players.GetNationalities();
                return View(player);
            }

            var playerData = this.players.Create(
                player.Nickname,
                player.FirstName,
                player.LastName,
                player.NationalityId);

            if (player.PictureFile != null && player.PictureFile.Length > 0)
            {
                try
                {
                    var url = await ImageOptimizer.SaveWebpAsync(
                        player.PictureFile,
                        env.WebRootPath,
                        Path.Combine("uploads", "PlayerProfiles"),
                        maxWidth: 512,
                        maxHeight: 512,
                        quality: 85);

                    this.data.Pictures.Add(new PlayerPicture
                    {
                        PlayerId = playerData,
                        PictureURL = url,
                        dateChanged = DateTime.UtcNow
                    });
                    await this.data.SaveChangesAsync();
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine("❌ IO Exception while saving file: " + ioEx);
                    ModelState.AddModelError("", "File write error: " + ioEx.Message);
                    player.Nationalities = this.players.GetNationalities();
                    return View(player);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ General Exception while saving file: " + ex);
                    ModelState.AddModelError("", "Unexpected error while saving uploaded file.");
                    player.Nationalities = this.players.GetNationalities();
                    return View(player);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Edit(int id)
        {
            var entity = this.data.Players.Find(id);
            if (entity == null)
                return NotFound();

            return View(new PlayerFormModel
            {
                Id = entity.Id,
                Nickname = entity.Nickname,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                NationalityId = entity.NationalityId,
                Nationalities = this.players.GetNationalities()
            });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PlayerFormModel model)
        {
            if (id != model.Id)
                return BadRequest();

            var entity = this.data.Players.Find(id);
            if (entity == null)
                return NotFound();

            if (!this.data.Nationalities.Any(c => c.Id == model.NationalityId))
            {
                ModelState.AddModelError(nameof(model.NationalityId), "This nationality isn't registered in the Database.");
            }

            if (!ModelState.IsValid)
            {
                model.Nationalities = this.players.GetNationalities();
                return View(model);
            }

            var ok = this.players.Edit(
                model.Id,
                model.Nickname,
                model.FirstName,
                model.LastName,
                model.NationalityId);
            if (!ok)
                return NotFound();

            if (model.PictureFile != null && model.PictureFile.Length > 0)
            {
                try
                {
                    var url = await ImageOptimizer.SaveWebpAsync(
                        model.PictureFile,
                        env.WebRootPath,
                        Path.Combine("uploads", "PlayerProfiles"),
                        maxWidth: 512,
                        maxHeight: 512,
                        quality: 85);

                    this.data.Pictures.Add(new PlayerPicture
                    {
                        PlayerId = model.Id,
                        PictureURL = url,
                        dateChanged = DateTime.UtcNow
                    });
                    await this.data.SaveChangesAsync();
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine("❌ IO Exception while saving file: " + ioEx);
                    ModelState.AddModelError("", "File write error: " + ioEx.Message);
                    model.Nationalities = this.players.GetNationalities();
                    return View(model);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ General Exception while saving file: " + ex);
                    ModelState.AddModelError("", "Unexpected error while saving uploaded file.");
                    model.Nationalities = this.players.GetNationalities();
                    return View(model);
                }
            }

            var information = model.Nickname;
            return RedirectToAction(nameof(Profile), new { id = model.Id, information });
        }

        public IActionResult Profile(int id, string information, string? game = null, int? gameProfileId = null)
        {
            var player = this.players.Profile(id, game, gameProfileId);

            if (player == null) return NotFound();

            if (information != player.GetInformation())
            {
                return BadRequest();
            }

            ViewBag.SelectedGame = game;
            ViewBag.SelectedGameProfileId = gameProfileId;
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
