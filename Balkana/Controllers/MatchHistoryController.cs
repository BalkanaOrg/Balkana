using Balkana.Data;
using Balkana.Models.Match;
using Balkana.Services.Matches;
using Balkana.Services.Matches.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Controllers
{
    public class MatchHistoryController : Controller
    {
        private readonly MatchHistoryService _history;
        private readonly ApplicationDbContext _db;
        private readonly Dictionary<string, IMatchImporter> _importers;

        public MatchHistoryController(MatchHistoryService history, ApplicationDbContext db, Dictionary<string, IMatchImporter> importers)
        {
            _history = history;
            _db = db;
            _importers = importers;
        }

        public async Task<IActionResult> Index(string source, string profileId)
        {
            var matches = await _history.GetHistoryAsync(source, profileId);
            return View(matches);
        }

        [HttpGet]
        public IActionResult Import(string source, string profileId, string matchId)
        {
            var vm = new MatchImportViewModel
            {
                Source = source,
                ProfileId = profileId,
                MatchId = matchId,
                Tournaments = _db.Tournaments
                    .Include(t => t.Game)
                    .Where(t => (source == "RIOT" && t.Game.ShortName == "LoL") ||
                                (source == "FACEIT" && t.Game.ShortName == "CS2"))
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.FullName
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult GetSeries(int tournamentId)
        {
            var series = _db.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Where(s => s.TournamentId == tournamentId)
                .Select(s => new
                {
                    id = s.Id,
                    text = s.TeamA.FullName + " vs " + s.TeamB.FullName + " (" + s.DatePlayed.ToShortDateString() + ")"
                })
                .ToList();

            return Json(series);
        }

        [HttpPost]
        public async Task<IActionResult> ImportMatch(MatchImportViewModel model)
        {
            var series = await _db.Series.FindAsync(model.SelectedSeriesId);
            if (series == null)
            {
                ModelState.AddModelError("", "Invalid series selected.");
                return View("Import", model);
            }

            var importer = _importers[model.Source]; // "RIOT" or "FACEIT"

            // Get one or more matches (per map in FACEIT BO3/BO5)
            var matches = await importer.ImportMatchAsync(model.MatchId, _db);

            foreach (var match in matches)
            {
                match.SeriesId = model.SelectedSeriesId;
                _db.Matches.Add(match);
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Details", "Series", new { id = model.SelectedSeriesId });
        }
    }
}
