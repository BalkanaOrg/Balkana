using AutoMapper;
using AutoMapper.QueryableExtensions;
using Balkana.Data;
using Balkana.Data.DTOs.Bracket;
using Balkana.Data.Models;
using Balkana.Models.Tournaments;
using Balkana.Services.Bracket;
using Balkana.Services.Teams.Models;
using Balkana.Services.Tournaments.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DoubleEliminationBracketService bracketService;
        private readonly IMapper mapper;
        private readonly AutoMapper.IConfigurationProvider con;
        private readonly IWebHostEnvironment env;
        //private readonly ITournamentService tournaments;

        public TournamentsController(ApplicationDbContext context, DoubleEliminationBracketService bracketService, IMapper mapper, IWebHostEnvironment env)
        {
            _context = context;
            this.bracketService = bracketService;
            //this.tournaments = tournaments;
            this.mapper = mapper;
            this.con= mapper.ConfigurationProvider;
            this.env = env;
        }

        // GET: Tournament/Index
        public async Task<IActionResult> Index()
        {
            var tournaments = await _context.Tournaments
                .Include(t => t.Organizer)
                .Include(t => t.Game)
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                .OrderByDescending(c=>c.StartDate)
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
                    .Where(tr =>
                        tr.StartDate <= tournament.StartDate &&
                        (tr.EndDate == null || tr.EndDate >= tournament.StartDate) &&
                        tr.Status == PlayerTeamStatus.Active)
                    .GroupBy(tr => tr.PlayerId)
                    .Select(g => g.OrderByDescending(tr => tr.StartDate).First().Player)
                    .ToList()
                })
                .ToListAsync();

            ViewData["ParticipatingTeams"] = participatingTeams;

            return View(tournament);
        }

        // GET: Tournament/Add
        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Add() 
        {
            var games = AllGames();
            var organizers = AllOrganizers();
            var eliminationTypes = AllEliminationTypes();

            var vm = new TournamentFormViewModel
            {
                Games = games,
                Organizers = organizers,
                EliminationTypes = eliminationTypes
            };
            return View(vm);
        } 

        // POST: Tournament/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Add(TournamentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var vm = new TournamentFormViewModel
                {
                    Games = AllGames(),
                    Organizers = AllOrganizers(),
                    EliminationTypes = AllEliminationTypes()
                };
                return View(vm);
            }

            string? logoPath = null;

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "Tournaments");
                Directory.CreateDirectory(uploadsFolder);

                var ext = Path.GetExtension(model.LogoFile.FileName);
                var finalFileName = $"{Guid.NewGuid()}{ext}";
                var finalPath = Path.Combine(uploadsFolder, finalFileName);

                // write to temp first
                var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);

                try
                {
                    Console.WriteLine($">>> Writing upload to temp: {tempFile}");
                    await using (var tempStream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await model.LogoFile.CopyToAsync(tempStream);
                        await tempStream.FlushAsync();
                    }

                    // move to final destination atomically
                    Console.WriteLine($">>> Moving temp file to final path: {finalPath}");
                    if (System.IO.File.Exists(finalPath))
                    {
                        Console.WriteLine($">>> Final path already exists, deleting: {finalPath}");
                        System.IO.File.Delete(finalPath);
                    }
                    System.IO.File.Move(tempFile, finalPath);

                    logoPath = $"/uploads/Tournaments/{finalFileName}";
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine("❌ IO Exception while saving file: " + ioEx);
                    try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch { }
                    ModelState.AddModelError("", "File write error: " + ioEx.Message);
                    var vm = new TournamentFormViewModel
                    {
                        Games = AllGames(),
                        Organizers = AllOrganizers(),
                        EliminationTypes = AllEliminationTypes()
                    };
                    return View(vm);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ General Exception while saving file: " + ex);
                    try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch { }
                    ModelState.AddModelError("", "Unexpected error while saving file.");
                    var vm = new TournamentFormViewModel
                    {
                        Games = AllGames(),
                        Organizers = AllOrganizers(),
                        EliminationTypes = AllEliminationTypes()
                    };
                    return View(vm);
                }
            }

            var tournament = new Tournament
            {
                FullName = model.FullName,
                ShortName = model.ShortName,
                OrganizerId = model.OrganizerId,
                Description = model.Description,
                StartDate = model.StartDate,
                BannerUrl = logoPath,
                Elimination = model.EliminationType,
                EndDate = model.EndDate,
                GameId = model.GameId
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Edit(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();

            var vm = new TournamentFormViewModel
            {
                Id = tournament.Id,
                FullName = tournament.FullName,
                ShortName = tournament.ShortName,
                OrganizerId = tournament.OrganizerId,
                Description = tournament.Description,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                PrizePool = tournament.PrizePool,
                BannerUrl = tournament.BannerUrl,
                EliminationType = tournament.Elimination,
                GameId = tournament.GameId,
                Games = AllGames(),
                Organizers = AllOrganizers(),
                EliminationTypes = AllEliminationTypes(),
                AvailableTeams = await _context.Teams
                    .Select(t => new TeamSelectItem { Id = t.Id, FullName = t.FullName })
                    .ToListAsync(),
                SelectedTeamIds = tournament.TournamentTeams.Select(tt => tt.TeamId).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Edit(TournamentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Games = AllGames();
                model.Organizers = AllOrganizers();
                model.EliminationTypes = AllEliminationTypes();
                model.AvailableTeams = await _context.Teams
                    .Select(t => new TeamSelectItem { Id = t.Id, FullName = t.FullName })
                    .ToListAsync();
                return View(model);
            }

            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams)
                .FirstOrDefaultAsync(t => t.Id == model.Id);

            if (tournament == null) return NotFound();

            // Update basic fields
            tournament.FullName = model.FullName;
            tournament.ShortName = model.ShortName;
            tournament.OrganizerId = model.OrganizerId;
            tournament.Description = model.Description;
            tournament.StartDate = model.StartDate;
            tournament.EndDate = model.EndDate;
            tournament.PrizePool = model.PrizePool;
            tournament.Elimination = model.EliminationType;
            tournament.GameId = model.GameId;

            // ✅ Update participating teams
            _context.TournamentTeams.RemoveRange(tournament.TournamentTeams);

            var newTeams = model.SelectedTeamIds
                .Select((teamId, index) => new TournamentTeam
                {
                    TournamentId = tournament.Id,
                    TeamId = teamId,
                    Seed = index + 1
                });

            await _context.TournamentTeams.AddRangeAsync(newTeams);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = tournament.Id });
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
        [Authorize(Roles = "Administrator,Moderator")]
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
        [Authorize(Roles = "Administrator,Moderator")]
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
                .Where(tr =>
                    tr.StartDate <= tournament.StartDate &&
                    (tr.EndDate == null || tr.EndDate >= tournament.StartDate) &&
                    tr.Status == PlayerTeamStatus.Active)
                .GroupBy(tr => tr.PlayerId)
                .Select(g => g.OrderByDescending(tr => tr.StartDate).First().Player)
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

        private IEnumerable<TournamentOrganizersServiceModel> AllOrganizers()
        {
            return _context.Organizers
                .ProjectTo<TournamentOrganizersServiceModel>(this.con)
                .ToList();
        }
        private IEnumerable<TournamentGamesServiceModel> AllGames()
        {
            return _context.Games
                .ProjectTo<TournamentGamesServiceModel>(this.con)
                .ToList();
        }
        private IEnumerable<SelectListItem>? AllEliminationTypes()
        {
            return Enum.GetValues(typeof(EliminationType))
                .Cast<EliminationType>()
                .Select(e => new SelectListItem
                {
                    Value = e.ToString(),
                    Text = e.ToString()
                });
        }
    }
}
