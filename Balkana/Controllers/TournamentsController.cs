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
                .Include(t => t.Placements).ThenInclude(p => p.Team)
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
                GameId = model.GameId,
                PrizePool = model.PrizePool,
                PointsConfiguration = model.PointsConfiguration,
                PrizeConfiguration = model.PrizeConfiguration
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
                PointsConfiguration = tournament.PointsConfiguration,
                PrizeConfiguration = tournament.PrizeConfiguration,
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
            tournament.PointsConfiguration = model.PointsConfiguration;
            tournament.PrizeConfiguration = model.PrizeConfiguration;

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
                .Include(t => t.Series)
                    .ThenInclude(s => s.Matches)
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

            // Matches - show all matches including TBD vs TBD
            var matches = tournament.Series
                .Select(s => new
                {
                    id = s.Id,
                    stage_id = 1, // single stage for simplicity
                    group_id = s.Bracket == BracketType.Upper ? 0 :
                   s.Bracket == BracketType.Lower ? 1 : 2,
                    round_id = s.Round,
                    number = s.Position,
                    status = s.isFinished ? "finished" : "open",
                    opponent1 = s.TeamA != null
                        ? new { 
                            id = s.TeamA.Id, 
                            name = s.TeamA.FullName, 
                            score = GetTeamScore(s, s.TeamA), 
                            result = GetTeamResult(s, s.TeamA) 
                        }
                        : new { id = -1, name = "TBD", score = 0, result = "" },
                    opponent2 = s.TeamB != null
                        ? new { 
                            id = s.TeamB.Id, 
                            name = s.TeamB.FullName, 
                            score = GetTeamScore(s, s.TeamB), 
                            result = GetTeamResult(s, s.TeamB) 
                        }
                        : new { id = -1, name = "TBD", score = 0, result = "" }
                }).ToList();

            // Stages - must include skipFirstRound and groups
            var stages = new[]
            {
                new
                {
                    id = stageId,
                    name = tournament.Elimination == EliminationType.Single ? "Single Elimination" : "Double Elimination",
                    type = tournament.Elimination == EliminationType.Single ? "single_elimination" : "double_elimination",
                    settings = new
                    {
                        skipFirstRound = false
                    },
                    groups = tournament.Elimination == EliminationType.Single 
                        ? new[] { new { id = 0, name = "Main Bracket" } }
                        : new[]
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

        [HttpGet("api/series/{id}/details")]
        public IActionResult GetSeriesDetails(int id)
        {
            var series = _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.WinnerTeam)
                .Include(s => s.Matches)
                    .ThenInclude(m => m.WinnerTeam)
                .Include(s => s.Matches)
                    .ThenInclude(m => ((MatchCS)m).Map)
                .FirstOrDefault(s => s.Id == id);

            if (series == null)
                return NotFound();

            var seriesData = new
            {
                id = series.Id,
                name = series.Name,
                round = series.Round,
                position = series.Position,
                bracket = series.Bracket.ToString(),
                isFinished = series.isFinished,
                teamA = series.TeamA != null ? new { id = series.TeamA.Id, fullName = series.TeamA.FullName } : null,
                teamB = series.TeamB != null ? new { id = series.TeamB.Id, fullName = series.TeamB.FullName } : null,
                winnerTeam = series.WinnerTeam != null ? new { id = series.WinnerTeam.Id, fullName = series.WinnerTeam.FullName } : null,
                matches = series.Matches.Select(m => new
                {
                    id = m.Id,
                    externalMatchId = m.ExternalMatchId,
                    source = m.Source,
                    playedAt = m.PlayedAt,
                    isCompleted = m.IsCompleted,
                    mapName = m is MatchCS csMatch ? (csMatch.Map?.Name ?? "No Map") : "N/A",
                    winnerTeam = m.WinnerTeam != null ? new { id = m.WinnerTeam.Id, fullName = m.WinnerTeam.FullName } : null
                }).OrderBy(m => m.playedAt).ToList()
            };

            return Ok(seriesData);
        }

        private int GetTeamScore(Series series, Team team)
        {
            if (!series.isFinished || !series.Matches.Any())
                return 0;

            // Count wins for this team in the series
            int wins = 0;
            foreach (var match in series.Matches)
            {
                if (match.IsCompleted)
                {
                    var winner = DetermineMatchWinner(match);
                    if (winner == team)
                    {
                        wins++;
                    }
                }
            }
            return wins;
        }

        private string GetTeamResult(Series series, Team team)
        {
            if (!series.isFinished)
                return "";

            var teamScore = GetTeamScore(series, team);
            var otherTeam = series.TeamA == team ? series.TeamB : series.TeamA;
            var otherScore = GetTeamScore(series, otherTeam);

            if (teamScore > otherScore)
                return "win";
            else if (teamScore < otherScore)
                return "loss";
            else
                return "draw";
        }

        private Team DetermineMatchWinner(Match match)
        {
            // Use the WinnerTeam directly from the match
            if (match.WinnerTeam != null)
            {
                return match.WinnerTeam;
            }

            // Fallback: determine winner from player statistics if WinnerTeam is not set
            if (match is MatchCS csMatch)
            {
                var teamAStats = csMatch.PlayerStats
                    .Where(ps => ps.Team == csMatch.TeamASourceSlot)
                    .OfType<PlayerStatistic_CS2>()
                    .ToList();
                var teamBStats = csMatch.PlayerStats
                    .Where(ps => ps.Team == csMatch.TeamBSourceSlot)
                    .OfType<PlayerStatistic_CS2>()
                    .ToList();

                if (teamAStats.Any() && teamBStats.Any())
                {
                    // Use rounds played to determine winner as fallback
                    var teamARounds = teamAStats.FirstOrDefault()?.RoundsPlayed ?? 0;
                    var teamBRounds = teamBStats.FirstOrDefault()?.RoundsPlayed ?? 0;
                    
                    if (teamARounds > teamBRounds)
                        return match.TeamA;
                    else if (teamBRounds > teamARounds)
                        return match.TeamB;
                }
            }

            // Fallback: return null if we can't determine the winner
            return null;
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

            if (!tournament.TournamentTeams.Any())
            {
                TempData["Error"] = "No teams added to tournament. Please add teams first.";
                return RedirectToAction("Details", new { id = tournamentId });
            }

            var service = new BracketService(_context, mapper);

            // Get teams ordered by their seed (1, 2, 3, etc.)
            var teams = await _context.TournamentTeams
                .Where(tt => tt.TournamentId == tournamentId)
                .Include(tt => tt.Team)
                .OrderBy(tt => tt.Seed)
                .Select(tt => tt.Team)
                .ToListAsync();

            var series = service.GenerateBracket(tournamentId, teams);

            // First, save all series without NextSeriesId relationships
            foreach (var s in series)
            {
                s.NextSeriesId = null; // Clear NextSeriesId temporarily
            }

            await _context.Series.AddRangeAsync(series);
            await _context.SaveChangesAsync();

            // Now wire up the NextSeriesId relationships after series have IDs
            if (tournament.Elimination == EliminationType.Single)
            {
                service.WireUpSeriesProgression(series);
            }
            else
            {
                var doubleEliminationService = new DoubleEliminationBracketService(_context, mapper);
                doubleEliminationService.WireUpDoubleEliminationProgression(series);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{tournament.Elimination} bracket generated with {teams.Count} teams!";
            return RedirectToAction("Details", new { id = tournamentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> DeleteBracket(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return NotFound();

            if (!tournament.Series.Any())
            {
                TempData["Error"] = "No bracket exists to delete.";
                return RedirectToAction("Details", new { id = tournamentId });
            }

            _context.Series.RemoveRange(tournament.Series);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Bracket deleted successfully.";
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

            // Generate placements from bracket results
            await GeneratePlacementsFromBracket(tournament);

            TempData["Success"] = $"Tournament '{tournament.FullName}' concluded and points awarded!";
            return RedirectToAction("Details", new { id });
        }

        private async Task GeneratePlacementsFromBracket(Tournament tournament)
        {
            // Clear existing placements
            _context.TournamentPlacements.RemoveRange(tournament.Placements);

            // Get all participating teams
            var participatingTeams = await _context.TournamentTeams
                .Include(tt => tt.Team)
                .Where(tt => tt.TournamentId == tournament.Id)
                .OrderBy(tt => tt.Seed)
                .Select(tt => tt.Team)
                .ToListAsync();

            // Get all series with match results
            var allSeries = await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.WinnerTeam)
                .Include(s => s.Matches)
                .Where(s => s.TournamentId == tournament.Id)
                .ToListAsync();

            var placements = new List<TournamentPlacement>();

            if (tournament.Elimination == EliminationType.Single)
            {
                await GenerateSingleEliminationPlacements(allSeries, participatingTeams, placements, tournament);
            }
            else
            {
                await GenerateDoubleEliminationPlacements(allSeries, participatingTeams, placements, tournament);
            }

            // Add placements to database
            foreach (var placement in placements)
            {
                _context.TournamentPlacements.Add(placement);
            }

            await _context.SaveChangesAsync();
        }

        private async Task GenerateSingleEliminationPlacements(List<Series> allSeries, List<Team> participatingTeams, List<TournamentPlacement> placements, Tournament tournament)
        {
            Console.WriteLine($"🎯 Starting placement generation for {participatingTeams.Count} teams");
            Console.WriteLine($"🎯 Participating teams: {string.Join(", ", participatingTeams.Select(t => $"{t.FullName} (ID: {t.Id})"))}");

            // Get all series ordered by round (highest first)
            var seriesByRound = allSeries
                .Where(s => s.Bracket == BracketType.Upper)
                .GroupBy(s => s.Round)
                .OrderByDescending(g => g.Key)
                .ToList();

            if (!seriesByRound.Any())
            {
                Console.WriteLine("❌ No series found for placement generation");
                return;
            }

            Console.WriteLine($"🎯 Series by round: {string.Join(", ", seriesByRound.Select(g => $"Round {g.Key}: {g.Count()} series"))}");

            // Track teams by elimination round
            var eliminatedTeams = new Dictionary<int, List<Team>>();
            var remainingTeams = new HashSet<Team>(participatingTeams);

            // Process rounds from highest to lowest to track eliminations
            foreach (var roundGroup in seriesByRound)
            {
                var roundEliminated = new List<Team>();
                
                Console.WriteLine($"🎯 Processing Round {roundGroup.Key} with {roundGroup.Count()} series");
                
                foreach (var series in roundGroup)
                {
                    Console.WriteLine($"🎯 Series {series.Id}: {series.TeamA?.FullName} vs {series.TeamB?.FullName}, Finished: {series.isFinished}, Winner: {series.WinnerTeam?.FullName}");
                    
                    if (series.isFinished && series.TeamA != null && series.TeamB != null && series.WinnerTeam != null)
                    {
                        var winner = series.WinnerTeam;
                        var loser = series.TeamA == winner ? series.TeamB : series.TeamA;
                        
                        if (loser != null && remainingTeams.Contains(loser))
                        {
                            roundEliminated.Add(loser);
                            remainingTeams.Remove(loser);
                            Console.WriteLine($"🎯 Team {loser.FullName} (ID: {loser.Id}) eliminated in Round {roundGroup.Key} by {winner.FullName}");
                        }
                    }
                    else if (series.isFinished && series.TeamA != null && series.TeamB != null && series.WinnerTeam == null)
                    {
                        Console.WriteLine($"⚠️ Series {series.Id} is finished but has no winner - skipping");
                    }
                    else if (!series.isFinished)
                    {
                        Console.WriteLine($"⚠️ Series {series.Id} is not finished - skipping");
                    }
                    else if (series.TeamA == null || series.TeamB == null)
                    {
                        Console.WriteLine($"⚠️ Series {series.Id} has null teams - skipping");
                    }
                }

                if (roundEliminated.Any())
                {
                    eliminatedTeams[roundGroup.Key] = roundEliminated;
                    Console.WriteLine($"🎯 Round {roundGroup.Key} eliminated: {string.Join(", ", roundEliminated.Select(t => $"{t.FullName} (ID: {t.Id})"))}");
                }
            }

            Console.WriteLine($"🎯 Remaining teams after elimination: {string.Join(", ", remainingTeams.Select(t => $"{t.FullName} (ID: {t.Id})"))}");

            // Create placements with proper shared positions
            int currentPlacement = 1;

            // 1st place - winner of final (remaining team)
            if (remainingTeams.Count == 1)
            {
                var winner = remainingTeams.First();
                placements.Add(CreatePlacement(tournament, winner, currentPlacement));
                Console.WriteLine($"🏆 1st place: {winner.FullName} (ID: {winner.Id})");
                currentPlacement++;
            }
            else if (remainingTeams.Count > 1)
            {
                Console.WriteLine($"❌ ERROR: Multiple teams remaining after elimination: {remainingTeams.Count}");
                // Handle this case by taking the first team as winner
                var winner = remainingTeams.First();
                placements.Add(CreatePlacement(tournament, winner, currentPlacement));
                Console.WriteLine($"🏆 1st place (forced): {winner.FullName} (ID: {winner.Id})");
                currentPlacement++;
            }

            // 2nd place - runner-up of final
            var finalRound = seriesByRound.First().Key;
            if (eliminatedTeams.ContainsKey(finalRound))
            {
                var finalEliminated = eliminatedTeams[finalRound];
                foreach (var team in finalEliminated)
                {
                    placements.Add(CreatePlacement(tournament, team, currentPlacement));
                    Console.WriteLine($"🥈 2nd place: {team.FullName} (ID: {team.Id})");
                }
                currentPlacement++;
            }

            // Shared placements for earlier rounds (3rd-4th, 5th-8th, 9th-12th, etc.)
            for (int i = 1; i < seriesByRound.Count; i++)
            {
                var round = seriesByRound[i];
                if (eliminatedTeams.ContainsKey(round.Key))
                {
                    var roundEliminated = eliminatedTeams[round.Key];
                    foreach (var team in roundEliminated)
                    {
                        placements.Add(CreatePlacement(tournament, team, currentPlacement));
                        Console.WriteLine($"🥉 {currentPlacement}th place: {team.FullName} (ID: {team.Id})");
                    }
                    currentPlacement++;
                }
            }

            // Ensure ALL teams are placed (fallback for any missing teams)
            var placedTeamIds = placements.Select(p => p.TeamId).ToHashSet();
            var unplacedTeams = participatingTeams.Where(t => !placedTeamIds.Contains(t.Id)).ToList();
            
            // Add any unplaced teams to the last placement group
            if (unplacedTeams.Any())
            {
                Console.WriteLine($"🎯 Found {unplacedTeams.Count} unplaced teams: {string.Join(", ", unplacedTeams.Select(t => $"{t.FullName} (ID: {t.Id})"))}");
                foreach (var team in unplacedTeams)
                {
                    placements.Add(CreatePlacement(tournament, team, currentPlacement));
                    Console.WriteLine($"🎯 Unplaced team {currentPlacement}th: {team.FullName} (ID: {team.Id})");
                }
            }

            // Debug logging to verify all teams are placed
            Console.WriteLine($"🎯 Placement Generation Summary:");
            Console.WriteLine($"   Total participating teams: {participatingTeams.Count}");
            Console.WriteLine($"   Total placements created: {placements.Count}");
            Console.WriteLine($"   Placed team IDs: [{string.Join(", ", placements.Select(p => p.TeamId).OrderBy(id => id))}]");
            Console.WriteLine($"   Participating team IDs: [{string.Join(", ", participatingTeams.Select(t => t.Id).OrderBy(id => id))}]");
            
            // Group placements by placement number for debugging
            var placementGroups = placements.GroupBy(p => p.Placement).OrderBy(g => g.Key);
            foreach (var group in placementGroups)
            {
                Console.WriteLine($"   Placement {group.Key}: {group.Count()} teams - {string.Join(", ", group.Select(p => $"Team {p.TeamId}"))}");
            }
            
            if (placements.Count != participatingTeams.Count)
            {
                Console.WriteLine($"❌ WARNING: Placement count mismatch! Expected {participatingTeams.Count}, got {placements.Count}");
            }
        }

        private async Task GenerateDoubleEliminationPlacements(List<Series> allSeries, List<Team> participatingTeams, List<TournamentPlacement> placements, Tournament tournament)
        {
            // Find grand final
            var grandFinal = allSeries
                .Where(s => s.Bracket == BracketType.GrandFinal)
                .FirstOrDefault();

            if (grandFinal?.isFinished == true && grandFinal.WinnerTeam != null)
            {
                // 1st place - winner of grand final
                placements.Add(CreatePlacement(tournament, grandFinal.WinnerTeam, 1));

                // 2nd place - loser of grand final
                var runnerUp = grandFinal.TeamA == grandFinal.WinnerTeam ? grandFinal.TeamB : grandFinal.TeamA;
                if (runnerUp != null)
                {
                    placements.Add(CreatePlacement(tournament, runnerUp, 2));
                }

                // 3rd place - winner of lower bracket final (if it exists)
                var lowerBracketFinal = allSeries
                    .Where(s => s.Bracket == BracketType.Lower)
                    .OrderByDescending(s => s.Round)
                    .FirstOrDefault();

                if (lowerBracketFinal?.isFinished == true && lowerBracketFinal.WinnerTeam != null)
                {
                    placements.Add(CreatePlacement(tournament, lowerBracketFinal.WinnerTeam, 3));
                }

                // For double elimination, we need more complex logic to determine other placements
                // This is a simplified version - in a full implementation, you'd track all elimination paths
                var remainingTeams = participatingTeams
                    .Where(t => !placements.Any(p => p.TeamId == t.Id))
                    .ToList();

                // Assign remaining teams to shared placements (4th-8th, etc.)
                int currentPlacement = 4;
                int teamsPerPlacement = 2; // Start with 4th-5th, then 6th-7th, etc.

                for (int i = 0; i < remainingTeams.Count; i += teamsPerPlacement)
                {
                    var teamsInThisPlacement = remainingTeams.Skip(i).Take(teamsPerPlacement);
                    foreach (var team in teamsInThisPlacement)
                    {
                        placements.Add(CreatePlacement(tournament, team, currentPlacement));
                    }
                    currentPlacement += teamsPerPlacement;
                }
            }

            // Ensure ALL teams are placed (fallback for any missing teams)
            var placedTeamIds = placements.Select(p => p.TeamId).ToHashSet();
            var unplacedTeams = participatingTeams.Where(t => !placedTeamIds.Contains(t.Id)).ToList();
            
            // Add any unplaced teams to the last placement group
            if (unplacedTeams.Any())
            {
                int lastPlacement = placements.Any() ? placements.Max(p => p.Placement) + 1 : 1;
                foreach (var team in unplacedTeams)
                {
                    placements.Add(CreatePlacement(tournament, team, lastPlacement));
                }
            }

            // Debug logging to verify all teams are placed
            Console.WriteLine($"🎯 Double Elimination Placement Generation Summary:");
            Console.WriteLine($"   Total participating teams: {participatingTeams.Count}");
            Console.WriteLine($"   Total placements created: {placements.Count}");
            Console.WriteLine($"   Placed team IDs: [{string.Join(", ", placements.Select(p => p.TeamId).OrderBy(id => id))}]");
            Console.WriteLine($"   Participating team IDs: [{string.Join(", ", participatingTeams.Select(t => t.Id).OrderBy(id => id))}]");
            
            if (placements.Count != participatingTeams.Count)
            {
                Console.WriteLine($"❌ WARNING: Double elimination placement count mismatch! Expected {participatingTeams.Count}, got {placements.Count}");
            }
        }

        private TournamentPlacement CreatePlacement(Tournament tournament, Team team, int placement)
        {
            return new TournamentPlacement
            {
                TournamentId = tournament.Id,
                TeamId = team.Id,
                Placement = placement,
                PointsAwarded = GetPointsForPlacement(tournament, placement),
                // Prize amount would be calculated here if needed
            };
        }

        private int GetPointsForPlacement(Tournament tournament, int placement)
        {
            // Try to parse the points configuration JSON
            if (!string.IsNullOrEmpty(tournament.PointsConfiguration))
            {
                try
                {
                    var pointsConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(tournament.PointsConfiguration);
                    if (pointsConfig != null && pointsConfig.ContainsKey(placement.ToString()))
                    {
                        return pointsConfig[placement.ToString()];
                    }
                }
                catch
                {
                    // Fall back to default if JSON parsing fails
                }
            }

            // Default point distribution if no configuration (scalable for any placement)
            return placement switch
            {
                1 => 500,
                2 => 325,
                3 => 200,
                4 => 125,
                5 => 100,
                6 => 75,
                7 => 50,
                8 => 25,
                9 => 20,
                10 => 15,
                11 => 12,
                12 => 10,
                13 => 8,
                14 => 6,
                15 => 4,
                16 => 2,
                _ => Math.Max(1, 20 - placement) // Dynamic calculation for higher placements
            };
        }

        private decimal GetPrizeForPlacement(Tournament tournament, int placement)
        {
            // Try to parse the prize configuration JSON
            if (!string.IsNullOrEmpty(tournament.PrizeConfiguration))
            {
                try
                {
                    var prizeConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, decimal>>(tournament.PrizeConfiguration);
                    if (prizeConfig != null && prizeConfig.ContainsKey(placement.ToString()))
                    {
                        return tournament.PrizePool * prizeConfig[placement.ToString()];
                    }
                }
                catch
                {
                    // Fall back to default if JSON parsing fails
                }
            }

            // Default prize distribution if no configuration (scalable for any placement)
            if (tournament.PrizePool > 0)
            {
                return placement switch
                {
                    1 => tournament.PrizePool * 0.50m, // 50%
                    2 => tournament.PrizePool * 0.30m, // 30%
                    3 => tournament.PrizePool * 0.20m, // 20%
                    4 => tournament.PrizePool * 0.10m, // 10%
                    5 => tournament.PrizePool * 0.05m, // 5%
                    6 => tournament.PrizePool * 0.03m, // 3%
                    7 => tournament.PrizePool * 0.02m, // 2%
                    8 => tournament.PrizePool * 0.01m, // 1%
                    _ => 0 // No prize for placements beyond 8th
                };
            }

            return 0;
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
        private bool IsByeMatch(Series series)
        {
            // A match is a bye if one team is null and the other is not
            // But we should still show TBD vs TBD matches
            return (series.TeamA == null && series.TeamB != null) || 
                   (series.TeamA != null && series.TeamB == null);
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
