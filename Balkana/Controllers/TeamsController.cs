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

namespace Balkana.Controllers
{
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext data;
        private readonly ITeamService teams;
        private readonly IMapper mapper;

        public TeamsController(ApplicationDbContext data, ITeamService teams, IMapper mapper)
        {
            this.data = data;
            this.teams = teams;
            this.mapper = mapper;
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
        public IActionResult Add(TeamFormModel team)
        {
            if(!this.teams.GameExists(team.GameId))
            {
                this.ModelState.AddModelError(nameof(team.GameId), "Game doesn't exist.");
            }

            if(ModelState.ErrorCount > 1)
            {
                team.CategoriesGames = this.teams.AllGames();

                return View(team);
            }

            var teamId = this.teams.Create(
                team.FullName,
                team.Tag,
                team.LogoURL,
                team.YearFounded,
                team.GameId);


            var tteam = this.teams.Details(teamId);
            string teamInformation = tteam.GetInformation();
            //TempData[GlobalMessageKey] = "This team has been added to the database.";

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
