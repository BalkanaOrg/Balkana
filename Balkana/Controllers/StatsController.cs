using Balkana.Data;
using Balkana.Data.Models;
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
                               ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim().Contains(query))
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

        [HttpGet]
        public async Task<IActionResult> YearlyGameStats(int gameId, int year)
        {
            try
            {
                // Validate game exists
                var game = await _context.Games.FindAsync(gameId);
                if (game == null)
                    return NotFound();

                // Calculate date range for the year
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                // Get all tournaments for this game in this year
                var tournaments = await _context.Tournaments
                    .Where(t => t.GameId == gameId && 
                               t.StartDate.Year == year)
                    .Select(t => t.Id)
                    .ToListAsync();

                if (!tournaments.Any())
                {
                    return View("YearlyGameStats", new YearlyGameStatsViewModel
                    {
                        GameId = gameId,
                        GameName = game.FullName,
                        Year = year,
                        PlayerStats = new List<YearlyPlayerStatsModel>()
                    });
                }

                // Get all player stats from matches in these tournaments within the year
                var playerStatsQuery = _context.PlayerStats
                    .Include(ps => ps.Match)
                    .ThenInclude(m => m.Series)
                    .ThenInclude(s => s.Tournament)
                    .Where(ps => ps.Match != null && 
                                 ps.Match.Series != null && 
                                 tournaments.Contains(ps.Match.Series.TournamentId) &&
                                 ps.Match.PlayedAt >= startDate &&
                                 ps.Match.PlayedAt <= endDate);

                var allStats = await playerStatsQuery.ToListAsync();

                // Group by player UUID to aggregate stats
                var playerGroups = allStats
                    .GroupBy(ps => ps.PlayerUUID)
                    .ToList();

                var yearlyPlayerStats = new List<YearlyPlayerStatsModel>();

                foreach (var group in playerGroups)
                {
                    var playerStats = group.ToList();
                    
                    // Get player information from GameProfile
                    var gameProfile = await _context.GameProfiles
                        .Include(gp => gp.Player)
                        .FirstOrDefaultAsync(gp => gp.UUID == group.Key);

                    if (gameProfile == null) continue;

                    // Count unique tournaments and maps (matches)
                    var tournamentIds = playerStats
                        .Where(ps => ps.Match?.Series != null)
                        .Select(ps => ps.Match.Series.TournamentId)
                        .Distinct()
                        .ToList();
                    
                    var tournamentCount = tournamentIds.Count;
                    var mapCount = playerStats.Count;

                    // Separate CS2 and LoL stats
                    var cs2Stats = playerStats.OfType<PlayerStatistic_CS2>().ToList();
                    var lolStats = playerStats.OfType<PlayerStatistic_LoL>().ToList();

                    var yearlyPlayer = new YearlyPlayerStatsModel
                    {
                        PlayerId = gameProfile.PlayerId,
                        PlayerName = gameProfile.Player.FirstName + " " + gameProfile.Player.LastName,
                        PlayerNickname = gameProfile.Player.Nickname,
                        TournamentCount = tournamentCount,
                        MapCount = mapCount
                    };

                    // Aggregate CS2 stats if available
                    if (cs2Stats.Any())
                    {
                        var totalKills = cs2Stats.Sum(s => s.Kills);
                        var totalDeaths = cs2Stats.Sum(s => s.Deaths);
                        var totalAssists = cs2Stats.Sum(s => s.Assists);
                        var totalDamage = cs2Stats.Sum(s => s.Damage);
                        var totalRounds = cs2Stats.Sum(s => s.RoundsPlayed);

                        yearlyPlayer.CS2Stats = new CS2StatsModel
                        {
                            TotalKills = totalKills,
                            TotalDeaths = totalDeaths,
                            TotalAssists = totalAssists,
                            TotalDamage = totalDamage,
                            TotalRounds = totalRounds,
                            KDRatio = totalKills / (double)Math.Max(totalDeaths, 1),
                            ADR = totalDamage / (double)Math.Max(totalRounds, 1),
                            HLTVRating = cs2Stats.Average(s => s.HLTV1),
                            KAST = (int)cs2Stats.Average(s => s.KAST),
                            Headshots = cs2Stats.Sum(s => s.HSkills),
                            FirstKills = cs2Stats.Sum(s => s.FK),
                            FirstDeaths = cs2Stats.Sum(s => s.FD),
                            MultiKills = cs2Stats.Sum(s => s._2k + s._3k + s._4k + s._5k),
                            Clutches = cs2Stats.Sum(s => s._1v1 + s._1v2 + s._1v3 + s._1v4 + s._1v5),
                            _5k = cs2Stats.Sum(s => s._5k),
                            _4k = cs2Stats.Sum(s => s._4k),
                            _3k = cs2Stats.Sum(s => s._3k),
                            _2k = cs2Stats.Sum(s => s._2k),
                            _1k = cs2Stats.Sum(s => s._1k),
                            _1v1 = cs2Stats.Sum(s => s._1v1),
                            _1v2 = cs2Stats.Sum(s => s._1v2),
                            _1v3 = cs2Stats.Sum(s => s._1v3),
                            _1v4 = cs2Stats.Sum(s => s._1v4),
                            _1v5 = cs2Stats.Sum(s => s._1v5),
                            SniperKills = cs2Stats.Sum(s => s.SniperKills),
                            PistolKills = cs2Stats.Sum(s => s.PistolKills),
                            KnifeKills = cs2Stats.Sum(s => s.KnifeKills),
                            WallbangKills = cs2Stats.Sum(s => s.WallbangKills),
                            CollateralKills = cs2Stats.Sum(s => s.CollateralKills),
                            NoScopeKills = cs2Stats.Sum(s => s.NoScopeKills),
                            Flashes = cs2Stats.Sum(s => s.Flashes),
                            UtilityUsage = cs2Stats.Sum(s => s.UtilityUsage)
                        };

                        yearlyPlayer.HLTVRating = yearlyPlayer.CS2Stats.HLTVRating;
                    }

                    // Aggregate LoL stats if available
                    if (lolStats.Any())
                    {
                        var totalKills = lolStats.Sum(s => s.Kills ?? 0);
                        var totalDeaths = lolStats.Sum(s => s.Deaths ?? 0);
                        var totalAssists = lolStats.Sum(s => s.Assists ?? 0);
                        var totalGoldEarned = lolStats.Sum(s => s.GoldEarned);
                        var totalCreepScore = lolStats.Sum(s => s.CreepScore);
                        var totalVisionScore = lolStats.Sum(s => s.VisionScore);

                        yearlyPlayer.LoLStats = new LoLStatsModel
                        {
                            TotalKills = totalKills,
                            TotalDeaths = totalDeaths,
                            TotalAssists = totalAssists,
                            TotalGoldEarned = totalGoldEarned,
                            TotalCreepScore = totalCreepScore,
                            TotalVisionScore = totalVisionScore,
                            KDRatio = totalKills / (double)Math.Max(totalDeaths, 1),
                            KPARatio = (totalKills + totalAssists) / (double)Math.Max(totalDeaths, 1),
                            GoldPerMinute = totalGoldEarned / (double)Math.Max(lolStats.Count * 30, 1),
                            CSPerMinute = totalCreepScore / (double)Math.Max(lolStats.Count * 30, 1),
                            TotalDamageToChampions = lolStats.Sum(s => s.TotalDamageToChampions ?? 0),
                            TotalDamageToObjectives = lolStats.Sum(s => s.TotalDamageToObjectives ?? 0)
                        };

                        // For LoL, use KDA ratio as rating equivalent if no CS2 stats
                        if (yearlyPlayer.CS2Stats == null)
                        {
                            yearlyPlayer.HLTVRating = yearlyPlayer.LoLStats.KDRatio;
                        }
                    }

                    yearlyPlayerStats.Add(yearlyPlayer);
                }

                // Sort by: TournamentCount (desc), MapCount (desc), HLTVRating (desc)
                yearlyPlayerStats = yearlyPlayerStats
                    .OrderByDescending(p => p.TournamentCount)
                    .ThenByDescending(p => p.MapCount)
                    .ThenByDescending(p => p.HLTVRating)
                    .ToList();

                var viewModel = new YearlyGameStatsViewModel
                {
                    GameId = gameId,
                    GameName = game.FullName,
                    Year = year,
                    PlayerStats = yearlyPlayerStats
                };

                return View("YearlyGameStats", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in YearlyGameStats: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return View("YearlyGameStats", new YearlyGameStatsViewModel
                {
                    GameId = gameId,
                    GameName = "Unknown",
                    Year = year,
                    PlayerStats = new List<YearlyPlayerStatsModel>()
                });
            }
        }
    }
}
