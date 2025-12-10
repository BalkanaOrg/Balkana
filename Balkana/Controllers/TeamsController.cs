using AutoMapper;
using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Data.Infrastructure.Extensions;
using Balkana.Data.Infrastructure;
using Balkana.Models.Teams;
using Balkana.Services.Teams;
using Balkana.Services.Teams.Models;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using IOFile = System.IO.File;
using Balkana.Services.Images;
using System.IO;

namespace Balkana.Controllers
{
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext data;
        private readonly ITeamService teams;
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment env;

        public TeamsController(ApplicationDbContext data, ITeamService teams, IMapper mapper, IWebHostEnvironment env)
        {
            this.data = data;
            this.teams = teams;
            this.mapper = mapper;
            this.env = env;
            Console.WriteLine(">>> WebRootPath = " + env.WebRootPath);
        }

        public IActionResult Index([FromQuery] AllTeamsQueryModel query)
        {
            var queryResult = this.teams.All(
                query.Game,
                query.SearchTerm,
                query.Year,
                query.CurrentPage,
                AllTeamsQueryModel.TeamsPerPage);

            var teamGames = this.teams.GetAllGames();
            var availableYears = this.teams.GetAvailableYears();
            var absoluteNumberTeams = this.teams.AbsoluteNumberOfTeams();

            query.Games = teamGames;
            query.AvailableYears = availableYears;
            query.TotalTeams = queryResult.TotalTeams;
            query.Teams = queryResult.Teams;
            query.AbsoluteNumberOfTeams = absoluteNumberTeams;

            query.SelectedGame = query.Game;
            
            // Calculate starting rank for the partial view
            ViewBag.StartRank = (query.CurrentPage - 1) * AllTeamsQueryModel.TeamsPerPage + 1;

            return View(query);
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Add()
        {
            //if(!this.User.IsAdmin())
            //{
            //    return RedirectToAction("Index", "Teams");
            //}
            return View(new TeamFormModel
            {
                CategoriesGames = this.teams.AllGames()
            });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Add(TeamFormModel team)
        {
            Console.WriteLine("ModelState.IsValid = " + ModelState.IsValid);
            Console.WriteLine("LogoFile = " + (team.LogoFile?.FileName ?? "NULL"));

            if (!this.teams.GameExists(team.GameId))
            {
                this.ModelState.AddModelError(nameof(team.GameId), "Game doesn't exist.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Validation failed");
                team.CategoriesGames = this.teams.AllGames();
                return View(team);
            }

            string? logoPath = null;

            if (team.LogoFile != null && team.LogoFile.Length > 0)
            {
                try
                {
                    logoPath = await ImageOptimizer.SaveWebpAsync(
                        team.LogoFile,
                        env.WebRootPath,
                        Path.Combine("uploads", "TeamLogos"),
                        maxWidth: 512,
                        maxHeight: 512,
                        quality: 85);
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine("❌ IO Exception while saving file: " + ioEx);
                    ModelState.AddModelError("", "File write error: " + ioEx.Message);
                    team.CategoriesGames = this.teams.AllGames();
                    return View(team);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ General Exception while saving file: " + ex);
                    ModelState.AddModelError("", "Unexpected error while saving file.");
                    team.CategoriesGames = this.teams.AllGames();
                    return View(team);
                }
            }


            var teamId = this.teams.Create(
                team.FullName,
                team.Tag,
                logoPath ?? string.Empty,
                team.YearFounded,
                team.GameId);

            Console.WriteLine("Team created with ID = " + teamId);

            var tteam = this.teams.Details(teamId);
            string teamInformation = tteam.GetInformation();

            return RedirectToAction(nameof(Details), new { id = teamId, information = teamInformation });
        }

        public IActionResult Details(int id, string information)
        {
            var team = this.teams.Details(id);

            if(information != team.GetInformation())
            {
                return BadRequest();
            }

            team.Players = this.teams.AllPlayers(id);

            return View(team);
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Edit(int id)
        {
            var team = this.teams.Details(id);
            if (team == null) return NotFound();

            var model = new TeamFormModel
            {
                FullName = team.FullName,
                Tag = team.Tag,
                LogoPath = team.LogoURL, // show current logo
                GameId = team.GameId,
                YearFounded = team.yearFounded,
                CategoriesGames = this.teams.AllGames()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Edit(int id, TeamFormModel model)
        {
            if (!this.teams.GameExists(model.GameId))
            {
                this.ModelState.AddModelError(nameof(model.GameId), "Game doesn't exist.");
            }

            if (!ModelState.IsValid)
            {
                model.CategoriesGames = this.teams.AllGames();
                return View(model);
            }

            string? logoPath = model.LogoPath; // keep old logo by default

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                try
                {
                    logoPath = await ImageOptimizer.SaveWebpAsync(
                        model.LogoFile,
                        env.WebRootPath,
                        Path.Combine("uploads", "TeamLogos"),
                        maxWidth: 512,
                        maxHeight: 512,
                        quality: 85);
                }
                catch
                {
                    ModelState.AddModelError("", "Error saving uploaded file.");
                    model.CategoriesGames = this.teams.AllGames();
                    return View(model);
                }
            }

            // Update the team in your service
            this.teams.Update(
                id,
                model.FullName,
                model.Tag,
                logoPath ?? string.Empty,
                model.YearFounded,
                model.GameId);

            var updated = this.teams.Details(id);
            string info = updated.GetInformation();

            return RedirectToAction(nameof(Details), new { id, information = info });
        }

    }
}
