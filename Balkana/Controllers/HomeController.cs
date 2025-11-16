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
    }
}