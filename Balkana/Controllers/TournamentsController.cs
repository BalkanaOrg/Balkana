using Balkana.Data.Models;
using Balkana.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Balkana.Data.DTOs.Bracket;
using Balkana.Models.Tournaments;
using Balkana.Services.Bracket;

namespace Balkana.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DoubleEliminationBracketService bracketService;

        public TournamentsController(ApplicationDbContext context, DoubleEliminationBracketService bracketService)
        {
            _context = context;
            this.bracketService = bracketService;
        }

        // GET: Tournament/Index
        public async Task<IActionResult> Index()
        {
            var tournaments = await _context.Tournaments
                .Include(t => t.Organizer)
                .Include(t => t.Game)
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                .ToListAsync();

            return View(tournaments);
        }

        // GET: Tournament/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Organizer)
                .Include(t => t.Game)
                .Include(t => t.TournamentTeams).ThenInclude(tt => tt.Team) // include teams here
                .Include(t => t.Series).ThenInclude(s => s.TeamA)
                .Include(t => t.Series).ThenInclude(s => s.TeamB)
                .Include(t => t.Series).ThenInclude(s => s.NextSeries)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();   // move this up

            ViewData["OrderedSeries"] = tournament.Series
                .OrderBy(s => s.Bracket)
                .ThenBy(s => s.Round)
                .ThenBy(s => s.Position)
                .ToList();

            ViewData["ParticipatingTeams"] = await _context.TournamentTeams
                .Where(tt => tt.TournamentId == id)
                .Include(tt => tt.Team)
                    .ThenInclude(t => t.Transfers)         // Include the transfers
                        .ThenInclude(tr => tr.Player)     // Include the players
                .OrderBy(tt => tt.Seed)
                .Select(tt => tt.Team)
                .ToListAsync();

            return View(tournament);
        }

        // GET: Tournament/Add
        public IActionResult Add() => View(new TournamentFormViewModel());

        // POST: Tournament/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(TournamentFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var tournament = new Tournament
            {
                FullName = model.FullName,
                ShortName = model.ShortName,
                OrganizerId = model.OrganizerId,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                GameId = model.GameId
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("api/tournaments/{id}/bracket")]
        public IActionResult GetBracket(int id)
        {
            // Fetch tournament with teams and series
            var tournament = _context.Tournaments
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                .Include(t => t.Series)
                    .ThenInclude(s => s.TeamA)
                .Include(t => t.Series)
                    .ThenInclude(s => s.TeamB)
                .FirstOrDefault(t => t.Id == id);

            if (tournament == null)
                return NotFound();

            // Participants
            var participants = tournament.TournamentTeams
            .OrderBy(tt => tt.Seed)
            .Select(tt => new
            {
                id = tt.Team.Id,
                name = tt.Team.FullName
            })
            .ToList();

            // Add dummy participant for TBD slots
            participants.Add(new { id = -1, name = "TBD" });

            // Stage ID
            var stageId = 1;

            // Matches
            var matches = tournament.Series.Select(s => new
            {
                id = s.Id,
                stage_id = 1, // single stage for simplicity
                group_id = s.Bracket == BracketType.Upper ? 0 :
               s.Bracket == BracketType.Lower ? 1 : 2,
                round_id = s.Round,
                number = s.Position,
                status = "open", // or "finished", "in_progress" depending on your series
                opponent1 = s.TeamA != null
                    ? new { id = s.TeamA.Id, name = s.TeamA.FullName, score = 0, result = "" }
                    : new { id = -1, name = "TBD", score = 0, result = "" },
                opponent2 = s.TeamB != null
                    ? new { id = s.TeamB.Id, name = s.TeamB.FullName, score = 0, result = "" }
                    : new { id = -1, name = "TBD", score = 0, result = "" }
            }).ToList();

            // Stages - must include skipFirstRound and groups
            var stages = new[]
            {
                new
                {
                    id = stageId,
                    name = "Double Elimination",
                    type = "double_elimination",
                    settings = new
                    {
                        skipFirstRound = false
                    },
                    groups = new[]
                    {
                        new { id = 0, name = "Upper Bracket" },
                        new { id = 1, name = "Lower Bracket" },
                        new { id = 2, name = "Grand Final" }
                    }
                }
            };

            return Ok(new
            {
                participants,
                matches,
                matchGames = new object[0],
                stages
            });
        }





        // GET: /Tournaments/AddTeams/5
        public async Task<IActionResult> AddTeams(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams)
                .ThenInclude(tt => tt.Team)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();

            var vm = new TournamentTeamsViewModel
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.FullName,
                SelectedTeamIds = tournament.TournamentTeams.Select(tt => tt.TeamId).ToList(),
                AvailableTeams = await _context.Teams
                    .Select(t => new TeamSelectItem { Id = t.Id, FullName = t.FullName })
                    .ToListAsync()
            };

            return View(vm);
        }

        // POST: /Tournaments/AddTeams
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeams(TournamentTeamsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var allErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Count > 0)
                    .Select(kvp => new
                    {
                        Field = kvp.Key,
                        Errors = kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    });

                return Json(allErrors); // <-- temporarily return JSON so you can see errors in browser
            }

            // Remove old teams
            var existing = _context.TournamentTeams
                .Where(tt => tt.TournamentId == model.TournamentId);
            _context.TournamentTeams.RemoveRange(existing);

            // Add new selected teams
            var newEntries = model.SelectedTeamIds
                .Select((teamId, index) => new TournamentTeam
                {
                    TournamentId = model.TournamentId,
                    TeamId = teamId,
                    Seed = index + 1
                });

            await _context.TournamentTeams.AddRangeAsync(newEntries);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = model.TournamentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBracket(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return NotFound();
            if (tournament.Series.Any())
            {
                TempData["Error"] = "Bracket already exists.";
                return RedirectToAction("Details", new { id = tournamentId });
            }

            var teams = tournament.TournamentTeams
                .OrderBy(tt => tt.Seed)
                .Select(tt => tt.Team)
                .ToList();

            var series = bracketService.Generate8TeamDoubleElimination(teams, tournamentId, tournament.ShortName);

            await _context.Series.AddRangeAsync(series);
            await _context.SaveChangesAsync();

            TempData["Success"] = "8-team double elimination bracket generated!";
            return RedirectToAction("Details", new { id = tournamentId });
        }
    }
}
