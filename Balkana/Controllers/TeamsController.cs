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
        }

        public IActionResult Index([FromQuery] AllTeamsQueryModel query)
        {
            var queryResult = this.teams.All(
                query.Game,
                query.SearchTerm,
                query.CurrentPage,
                AllTeamsQueryModel.TeamsPerPage);

            var teamGames = this.teams.GetAllGames();
            var absoluteNumberTeams = this.teams.AbsoluteNumberOfTeams();

            query.Games = teamGames;
            query.TotalTeams = queryResult.TotalTeams;
            query.Teams = queryResult.Teams;
            query.AbsoluteNumberOfTeams = absoluteNumberTeams;

            query.SelectedGame = query.Game;

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
            if (!this.teams.GameExists(team.GameId))
            {
                this.ModelState.AddModelError(nameof(team.GameId), "Game doesn't exist.");
            }

            if (!ModelState.IsValid)
            {
                team.CategoriesGames = this.teams.AllGames();
                return View(team);
            }

            string? logoPath = null;

            if (team.LogoFile != null && team.LogoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "TeamLogos");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(team.LogoFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await team.LogoFile.CopyToAsync(stream);
                }

                // relative path to store in DB
                logoPath = $"/uploads/TeamLogos/{fileName}";
            }

            var teamId = this.teams.Create(
                team.FullName,
                team.Tag,
                logoPath ?? string.Empty,
                team.YearFounded,
                team.GameId);

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
    }
}
