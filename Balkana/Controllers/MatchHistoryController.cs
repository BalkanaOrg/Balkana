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

        public async Task<IActionResult> Index(string source, string profileId, int? clubId)
        {
            if (source == "FACEIT")
            {
                if (!clubId.HasValue)
                {
                    // no club selected -> show club selection page
                    var clubs = await _db.FaceitClubs
                        .Select(c => new SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = c.Name
                        })
                        .ToListAsync();

                    ViewBag.Source = source;
                    return View("SelectClub", clubs);
                }

                // get the hub id from DB
                var club = await _db.FaceitClubs.FindAsync(clubId.Value);
                if (club == null) return NotFound("Invalid club");

                // tell importer which club to use
                var importer = (FaceitMatchImporter)_importers["FACEIT"];
                importer.SetHubId(club.FaceitId);

                var matches = await importer.GetMatchHistoryAsync(profileId);
                ViewBag.Source = source;
                return View(matches);
            }

            // RIOT flow stays the same
            var riotMatches = await _history.GetHistoryAsync(source, profileId);
            ViewBag.Source = source;
            return View(riotMatches);
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

            if (source == "FACEIT")
            {
                vm.Clubs = _db.FaceitClubs
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToList();
            }

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
                    text = s.TeamA.FullName + " vs " + s.TeamB.FullName + " (" + s.DatePlayed.ToShortDateString() + ") " + s.Name
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

            var importer = _importers[model.Source];

            // If FaceIt, pass the chosen ClubId
            if (model.Source == "FACEIT" && model.SelectedClubId.HasValue)
            {
                var club = await _db.FaceitClubs.FindAsync(model.SelectedClubId.Value);
                if (club == null)
                {
                    ModelState.AddModelError("", "Invalid FaceIt Club selected.");
                    return View("Import", model);
                }

                // 👉 update the importer to accept dynamic HubId instead of config
                ((FaceitMatchImporter)importer).SetHubId(club.FaceitId);
            }

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
