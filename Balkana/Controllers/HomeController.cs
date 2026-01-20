using AutoMapper;
using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Balkana.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext data;
        private readonly IMapper mapper;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext data, IMapper mapper)
        {
            _logger = logger;
            this.data = data;
            this.mapper = mapper;
        }

        public IActionResult Index()
        {
            var prizePool = data.Tournaments.Sum(c => c.PrizePool);
            int prizePoolEuros = (int)Math.Floor(prizePool);
            ViewData["PrizePool"] = prizePoolEuros;

            var players = data.Players.Count();
            ViewData["PlayerCount"] = players;

            var games = data.Games.Count();
            ViewData["GamesCount"] = games;

            var latestArticles = FetchLatestArticles();
            return View(latestArticles);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            ViewData["Title"] = "About Balkana";
            ViewData["Description"] = "Learn more about the people behind Balkana, our mission, and what we stand for in the Balkan esports ecosystem.";
            ViewData["Keywords"] = "Balkana, about us, founders, mission, values, esports, Balkans, gaming community";

            return View();
        }

        public async Task<IActionResult> LeagueOfLegends()
        {
            var tournaments = await FetchLatestTournamentsByGame("League of Legends");
            ViewData["Title"] = "League of Legends - Balkana";
            ViewData["Description"] = "Discover League of Legends tournaments organized by Balkana. Join the ultimate MOBA competition in the Balkans.";
            ViewData["Keywords"] = "Balkana, League of Legends, LoL, MOBA, esports tournament, Balkans";
            return View(tournaments);
        }

        public async Task<IActionResult> CounterStrike()
        {
            var tournaments = await FetchLatestTournamentsByGame("Counter-Strike");
            ViewData["Title"] = "Counter-Strike - Balkana";
            ViewData["Description"] = "Explore Counter-Strike tournaments organized by Balkana. Experience tactical FPS competition at its finest.";
            ViewData["Keywords"] = "Balkana, Counter-Strike, CS2, CS:GO, FPS, esports tournament, Balkans";
            return View(tournaments);
        }

        public async Task<IActionResult> RainbowSixSiege()
        {
            var tournaments = await FetchLatestTournamentsByGame("Rainbow Six Siege");
            ViewData["Title"] = "Rainbow Six Siege - Balkana";
            ViewData["Description"] = "Check out Rainbow Six Siege tournaments organized by Balkana. Tactical 5v5 shooter competition in the Balkans.";
            ViewData["Keywords"] = "Balkana, Rainbow Six Siege, R6, tactical shooter, esports tournament, Balkans";
            return View(tournaments);
        }

        public async Task<IActionResult> Valorant()
        {
            var tournaments = await FetchLatestTournamentsByGame("VALORANT");
            ViewData["Title"] = "VALORANT - Balkana";
            ViewData["Description"] = "Join VALORANT tournaments organized by Balkana. Character-based tactical shooter competition in the Balkans.";
            ViewData["Keywords"] = "Balkana, VALORANT, tactical shooter, esports tournament, Balkans";
            return View(tournaments);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private List<Article> FetchLatestArticles()
        {
            return data.Articles
                .Where(a => a.Status == "Published")
                .Include(a => a.Author)
                .OrderByDescending(a => a.PublishedAt)
                .Take(4)
                .ToList();
        }

        private async Task<List<Tournament>> FetchLatestTournamentsByGame(string gameName)
        {
            return await data.Tournaments
                .Where(t => t.Game != null && t.Game.FullName == gameName)
                .Include(t => t.Game)
                .Include(t => t.Organizer)
                .OrderByDescending(t => t.StartDate)
                .Take(5)
                .ToListAsync();
        }
    }
}