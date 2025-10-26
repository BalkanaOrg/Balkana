using Balkana.Data;
using Balkana.Models.Stats;
using Balkana.Services.Stats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Controllers
{
    public class StatsController : Controller
    {
        private readonly IStatsService _statsService;
        private readonly ApplicationDbContext _context;

        public StatsController(IStatsService statsService, ApplicationDbContext context)
        {
            _statsService = statsService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Player(int playerId, string? provider = null, DateTime? startDate = null, DateTime? endDate = null, int? gameId = null)
        {
            try
            {
                var request = new StatsRequestModel
                {
                    RequestType = StatsRequestType.Player,
                    PlayerId = playerId,
                    Provider = provider,
                    StartDate = startDate,
                    EndDate = endDate,
                    GameId = gameId
                };

                var stats = await _statsService.GetPlayerStatsAsync(request);
                return View("PlayerStats", stats);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in Player stats: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Return empty result with error message
                return View("PlayerStats", new List<PlayerStatsResponseModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Team(int teamId, string? provider = null, DateTime? startDate = null, DateTime? endDate = null, int? gameId = null, string? rosterType = null)
        {
            try
            {
                // Get team information
                var team = await _context.Teams
                    .Include(t => t.Game)
                    .FirstOrDefaultAsync(t => t.Id == teamId);

                if (team == null)
                    return NotFound();

                // Parse roster type
                var rosterTypeEnum = TeamRosterType.CurrentRoster;
                if (!string.IsNullOrEmpty(rosterType) && Enum.TryParse<TeamRosterType>(rosterType, true, out var parsedRosterType))
                {
                    rosterTypeEnum = parsedRosterType;
                }

                var request = new StatsRequestModel
                {
                    RequestType = StatsRequestType.Team,
                    TeamId = teamId,
                    Provider = provider,
                    StartDate = startDate,
                    EndDate = endDate,
                    GameId = gameId
                };

                var playerStats = await _statsService.GetTeamStatsAsync(request, rosterTypeEnum);
                var teamStats = await _statsService.GetTeamAggregatedStatsAsync(request, rosterTypeEnum);

                var viewModel = new TeamStatsViewModel
                {
                    TeamId = team.Id,
                    TeamName = team.FullName,
                    TeamTag = team.Tag,
                    GameName = team.Game.FullName,
                    LogoUrl = team.LogoURL,
                    RosterType = rosterTypeEnum,
                    PlayerStats = playerStats,
                    TeamStats = teamStats,
                    TotalPlayers = playerStats.Count,
                    TotalMatches = playerStats.Sum(p => p.TotalMatches),
                    FirstMatchDate = playerStats.Where(p => p.FirstMatchDate.HasValue).Min(p => p.FirstMatchDate),
                    LastMatchDate = playerStats.Where(p => p.LastMatchDate.HasValue).Max(p => p.LastMatchDate)
                };

                return View("TeamStats", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Team stats: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return View("TeamStats", new TeamStatsViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Series(int seriesId, string? provider = null, DateTime? startDate = null, DateTime? endDate = null, int? gameId = null)
        {
            try
            {
                var request = new StatsRequestModel
                {
                    RequestType = StatsRequestType.Series,
                    SeriesId = seriesId,
                    Provider = provider,
                    StartDate = startDate,
                    EndDate = endDate,
                    GameId = gameId
                };

                var stats = await _statsService.GetSeriesStatsAsync(request);
                return View("SeriesStats", stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Series stats: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return View("SeriesStats", new List<PlayerStatsResponseModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Tournament(int tournamentId, string? provider = null, DateTime? startDate = null, DateTime? endDate = null, int? gameId = null)
        {
            try
            {
                var request = new StatsRequestModel
                {
                    RequestType = StatsRequestType.Tournament,
                    TournamentId = tournamentId,
                    Provider = provider,
                    StartDate = startDate,
                    EndDate = endDate,
                    GameId = gameId
                };

                var stats = await _statsService.GetTournamentStatsAsync(request);
                return View("TournamentStatsTable", stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Tournament stats: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return View("TournamentStats", new List<PlayerStatsResponseModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query, string? provider = null, int? gameId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return Json(new { results = new List<object>() });

                var results = new List<object>();

                // Search players
                var players = await _context.Players
                    .Include(p => p.GameProfiles)
                    .Where(p => p.Nickname.Contains(query) || 
                               (p.FirstName + " " + p.LastName).Contains(query))
                    .Take(10)
                    .Select(p => new
                    {
                        type = "player",
                        id = p.Id,
                        name = p.Nickname,
                        fullName = p.FirstName + " " + p.LastName,
                        providers = p.GameProfiles.Select(gp => gp.Provider).ToList()
                    })
                    .ToListAsync();

                results.AddRange(players);

                // Search teams
                var teams = await _context.Teams
                    .Where(t => t.FullName.Contains(query) || t.Tag.Contains(query))
                    .Take(10)
                    .Select(t => new
                    {
                        type = "team",
                        id = t.Id,
                        name = t.FullName,
                        tag = t.Tag,
                        game = t.Game.FullName
                    })
                    .ToListAsync();

                results.AddRange(teams);

                // Search tournaments
                var tournaments = await _context.Tournaments
                    .Where(t => t.FullName.Contains(query) || t.ShortName.Contains(query))
                    .Take(10)
                    .Select(t => new
                    {
                        type = "tournament",
                        id = t.Id,
                        name = t.FullName,
                        shortName = t.ShortName,
                        game = t.Game.FullName,
                        startDate = t.StartDate
                    })
                    .ToListAsync();

                results.AddRange(tournaments);

                return Json(new { results });
            }
            catch (Exception ex)
            {
                return Json(new { results = new List<object>(), error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlayerProviders(int playerId)
        {
            var player = await _context.Players
                .Include(p => p.GameProfiles)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
                return Json(new { providers = new List<object>() });

            var providers = player.GameProfiles.Select(gp => new
            {
                provider = gp.Provider,
                uuid = gp.UUID,
                game = gp.Provider == "FACEIT" ? "CS2" : gp.Provider == "RIOT" ? "LoL" : "Unknown"
            }).ToList();

            return Json(new { providers });
        }

        [HttpGet]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                var games = await _context.Games
                    .Select(g => new { id = g.Id, name = g.FullName })
                    .ToListAsync();

                var providers = new[]
                {
                    new { value = "FACEIT", label = "FACEIT (CS2)" },
                    new { value = "RIOT", label = "Riot Games (LoL)" },
                    new { value = "MANUAL", label = "Manual Entry" }
                };

                return Json(new { games, providers });
            }
            catch (Exception ex)
            {
                return Json(new { games = new List<object>(), providers = new List<object>(), error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Stats(int? player = null, int? team = null, int? series = null, int? tournament = null, 
            string? provider = null, DateTime? startDate = null, DateTime? endDate = null, int? gameId = null)
        {
            try
            {
                var request = new StatsRequestModel
                {
                    Provider = provider,
                    StartDate = startDate,
                    EndDate = endDate,
                    GameId = gameId
                };

                List<PlayerStatsResponseModel> stats = new List<PlayerStatsResponseModel>();
                string statsType = "";
                string statsTitle = "";

                if (player.HasValue)
                {
                    request.RequestType = StatsRequestType.Player;
                    request.PlayerId = player.Value;
                    stats = await _statsService.GetPlayerStatsAsync(request);
                    statsType = "player";
                    
                    // Get player name for title
                    var playerEntity = await _context.Players.FindAsync(player.Value);
                    statsTitle = playerEntity != null ? $"{playerEntity.FirstName} {playerEntity.LastName} ({playerEntity.Nickname})" : $"Player {player.Value}";
                }
                else if (team.HasValue)
                {
                    request.RequestType = StatsRequestType.Team;
                    request.TeamId = team.Value;
                    stats = await _statsService.GetTeamStatsAsync(request);
                    statsType = "team";
                    
                    // Get team name for title
                    var teamEntity = await _context.Teams.FindAsync(team.Value);
                    statsTitle = teamEntity != null ? teamEntity.FullName : $"Team {team.Value}";
                }
                else if (series.HasValue)
                {
                    request.RequestType = StatsRequestType.Series;
                    request.SeriesId = series.Value;
                    stats = await _statsService.GetSeriesStatsAsync(request);
                    statsType = "series";
                    
                    // Get series name for title
                    var seriesEntity = await _context.Series
                        .Include(s => s.TeamA)
                        .Include(s => s.TeamB)
                        .FirstOrDefaultAsync(s => s.Id == series.Value);
                    statsTitle = seriesEntity != null ? seriesEntity.Name : $"Series {series.Value}";
                }
                else if (tournament.HasValue)
                {
                    request.RequestType = StatsRequestType.Tournament;
                    request.TournamentId = tournament.Value;
                    stats = await _statsService.GetTournamentStatsAsync(request);
                    statsType = "tournament";
                    
                    // Get tournament name for title
                    var tournamentEntity = await _context.Tournaments.FindAsync(tournament.Value);
                    statsTitle = tournamentEntity != null ? tournamentEntity.FullName : $"Tournament {tournament.Value}";
                }
                else
                {
                    // No parameters provided - show empty state
                    return View("Stats", new { 
                        Stats = new List<PlayerStatsResponseModel>(), 
                        StatsType = "", 
                        StatsTitle = "Statistics",
                        HasStats = false,
                        Error = (string?)null
                    });
                }

                return View("Stats", new { 
                    Stats = stats, 
                    StatsType = statsType, 
                    StatsTitle = statsTitle,
                    HasStats = stats.Any(),
                    Error = (string?)null
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Stats: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return View("Stats", new { 
                    Stats = new List<PlayerStatsResponseModel>(), 
                    StatsType = "", 
                    StatsTitle = "Statistics",
                    HasStats = false,
                    Error = ex.Message
                });
            }
        }
    }
}
