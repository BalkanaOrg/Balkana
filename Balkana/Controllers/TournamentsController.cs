using Balkana.Data.Models;
using Balkana.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Balkana.Data.DTOs.Bracket;
using Balkana.Models.Tournaments;
using Balkana.Services.Bracket;
using AutoMapper;

namespace Balkana.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DoubleEliminationBracketService bracketService;
        private readonly IMapper mapper;
        //private readonly ITournamentService tournaments;

        public TournamentsController(ApplicationDbContext context, DoubleEliminationBracketService bracketService, IMapper mapper)
        {
            _context = context;
            this.bracketService = bracketService;
            //this.tournaments = tournaments;
            this.mapper = mapper;
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

            var participatingTeams = await _context.TournamentTeams
                .Where(tt => tt.TournamentId == id)
                .Include(tt => tt.Team)
                    .ThenInclude(t => t.Transfers)
                        .ThenInclude(tr => tr.Player)
                .OrderBy(tt => tt.Seed)
                .Select(tt => new TournamentTeamRosterViewModel
                {
                    Team = tt.Team,
                    Players = tt.Team.Transfers
                        .Where(tr => tr.TransferDate <= tournament.StartDate)
                        .GroupBy(tr => tr.PlayerId)
                        .Select(g => g.OrderByDescending(tr => tr.TransferDate).First().Player)
                        .ToList()
                })
                .ToListAsync();

            ViewData["ParticipatingTeams"] = participatingTeams;

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
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return NotFound();
            if (tournament.Series.Any())
            {
                TempData["Error"] = "Bracket already exists.";
                return RedirectToAction("Details", new { id = tournamentId });
            }

            var service = new BracketService(_context, mapper);
            var series = service.GenerateBracket(tournamentId);

            await _context.Series.AddRangeAsync(series);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{tournament.Elimination} bracket generated!";
            return RedirectToAction("Details", new { id = tournamentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Conclude(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams).ThenInclude(tt => tt.Team)
                    .ThenInclude(t => t.Transfers).ThenInclude(tr => tr.Player)
                .Include(t => t.Placements) // if you track placements
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();

            // Example: placement → points mapping
            var pointsMap = new Dictionary<int, int>
            {
                { 1, 500 }, // 1st place = 500 pts
                { 2, 325 },
                { 3, 200 },
                { 4, 125 }
                // expand as needed
            };

            foreach (var placement in tournament.Placements)
            {
                if (!pointsMap.TryGetValue(placement.Placement, out var points))
                    continue;

                placement.PointsAwarded = points;

                // Get roster at tournament start
                var roster = placement.Team.Transfers
                    .Where(tr => tr.TransferDate <= tournament.StartDate)
                    .GroupBy(tr => tr.PlayerId)
                    .Select(g => g.OrderByDescending(tr => tr.TransferDate).First().Player)
                    .ToList();

                // Decide the "core" (for simplicity: top 3 players by presence)
                var corePlayers = roster.Take(3).ToList();

                // Find or create Core
                var core = await _context.Cores
                    .Include(c => c.Players)
                    .FirstOrDefaultAsync(c =>
                        corePlayers.All(cp => c.Players.Select(p => p.PlayerId).Contains(cp.Id))
                        && c.Players.Count == corePlayers.Count);

                if (core == null)
                {
                    core = new Core { Name = string.Join("/", corePlayers.Select(p => p.Nickname)) };
                    _context.Cores.Add(core);
                    await _context.SaveChangesAsync();

                    foreach (var p in corePlayers)
                    {
                        _context.CorePlayers.Add(new CorePlayer { CoreId = core.Id, PlayerId = p.Id });
                    }
                }

                // Award points
                _context.CoreTournamentPoints.Add(new CoreTournamentPoints
                {
                    CoreId = core.Id,
                    TournamentId = tournament.Id,
                    Points = points
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tournament '{tournament.FullName}' concluded and points awarded!";
            return RedirectToAction("Details", new { id });
        }

    }
}
