using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Balkana.Models.Series;
using Balkana.Services.Riot;

namespace Balkana.Controllers
{
    public class SeriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDDragonVersionService _ddragonVersion;

        public SeriesController(ApplicationDbContext context, IDDragonVersionService ddragonVersion)
        {
            _context = context;
            _ddragonVersion = ddragonVersion;
        }

        // GET: Series
        public async Task<IActionResult> Index()
        {
            var series = _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Tournament.Game)
                .Include(s => s.Tournament);
            return View(await series.ToListAsync());
        }

        // GET: Series/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Load base series data without CS-only map include (so LoL series won't break).
            var series = await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Tournament.Game)
                .Include(s => s.Tournament)
                .Include(s => s.Matches)
                    .ThenInclude(m => m.PlayerStats)
                .Include(s => s.WinnerTeam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (series == null) return NotFound();

            ViewData["GameShortName"] = series.Tournament?.Game?.ShortName ?? "";

            // For CS2, re-load with CS-only map include so existing CS UI logic stays intact.
            if (string.Equals(series.Tournament?.Game?.ShortName, "CS2", StringComparison.OrdinalIgnoreCase))
            {
                series = await _context.Series
                    .Include(s => s.TeamA)
                    .Include(s => s.TeamB)
                    .Include(s => s.Tournament.Game)
                    .Include(s => s.Tournament)
                    .Include(s => s.Matches)
                        .ThenInclude(m => m.PlayerStats)
                    .Include(s => s.Matches)
                        .ThenInclude(m => ((MatchCS)m).Map)
                    .Include(s => s.WinnerTeam)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }

            // Check if series has matches
            bool hasMatches = series.Matches.Any();

            if (!hasMatches)
            {
                // Get active players for both teams at the time of the series
                var teamAPlayers = await GetActivePlayersForTeam(series.TeamAId, series.DatePlayed);
                var teamBPlayers = await GetActivePlayersForTeam(series.TeamBId, series.DatePlayed);

                ViewData["TeamAPlayers"] = teamAPlayers;
                ViewData["TeamBPlayers"] = teamBPlayers;
                ViewData["HasMatches"] = false;
            }
            else
            {
                // Get match statistics and player performance
                if (string.Equals(series.Tournament?.Game?.ShortName, "LoL", StringComparison.OrdinalIgnoreCase))
                {
                    // LoL: per-game only (no combined "All Maps" stats).
                    var matchStats = await GetSeriesMatchStatsLoL(series);
                    ViewData["MatchStats"] = matchStats;
                    ViewData["LolNoCombinedMapStats"] = true;
                    ViewData["PlayerStats"] = await GetSeriesPlayerStatsLoL(series, mapId: 1);
                }
                else
                {
                    var matchStats = await GetSeriesMatchStats(series);
                    var playerStats = await GetSeriesPlayerStats(series);

                    ViewData["MatchStats"] = matchStats;
                    ViewData["PlayerStats"] = playerStats;
                }
                ViewData["HasMatches"] = true;
            }

            return View(series);
        }

        private async Task<List<Player>> GetActivePlayersForTeam(int? teamId, DateTime seriesDate)
        {
            if (teamId == null) return new List<Player>();

            return await _context.PlayerTeamTransfers
                .Include(tr => tr.Player)
                .Include(tr => tr.TeamPosition)
                .Where(tr => tr.TeamId == teamId &&
                           tr.Status == PlayerTeamStatus.Active &&
                           tr.StartDate <= seriesDate &&
                           (tr.EndDate == null || tr.EndDate >= seriesDate))
                .Select(tr => tr.Player)
                .Distinct()
                .ToListAsync();
        }

        private async Task<object> GetSeriesMatchStats(Series series)
        {
            var matches = series.Matches.ToList();
            var matchStats = new List<object>();

            foreach (var match in matches)
            {
                if (match is MatchCS csMatch)
                {
                    // Determine which team is which based on the actual teams in the series
                    var teamARounds = 0;
                    var teamBRounds = 0;
                    var teamAName = series.TeamA?.FullName ?? "Team A";
                    var teamBName = series.TeamB?.FullName ?? "Team B";

                    // Check if the winner matches TeamA or TeamB to determine correct score mapping
                    if (match.WinnerTeamId == series.TeamAId)
                    {
                        // TeamA won, so they should have the higher score
                        teamARounds = Math.Max(csMatch.TeamARounds ?? 0, csMatch.TeamBRounds ?? 0);
                        teamBRounds = Math.Min(csMatch.TeamARounds ?? 0, csMatch.TeamBRounds ?? 0);
                    }
                    else if (match.WinnerTeamId == series.TeamBId)
                    {
                        // TeamB won, so they should have the higher score
                        teamARounds = Math.Min(csMatch.TeamARounds ?? 0, csMatch.TeamBRounds ?? 0);
                        teamBRounds = Math.Max(csMatch.TeamARounds ?? 0, csMatch.TeamBRounds ?? 0);
                    }
                    else
                    {
                        // Fallback to original values if winner doesn't match either team
                        teamARounds = csMatch.TeamARounds ?? 0;
                        teamBRounds = csMatch.TeamBRounds ?? 0;
                    }

                    matchStats.Add(new
                    {
                        MatchId = match.Id,
                        MapId = csMatch.MapId,
                        MapName = csMatch.Map?.Name ?? "Unknown Map",
                        MapImage = csMatch.Map?.PictureURL ?? "/images/default-map.png",
                        TeamARounds = teamARounds,
                        TeamBRounds = teamBRounds,
                        TotalRounds = csMatch.TotalRounds ?? 0,
                        Winner = match.WinnerTeam?.FullName ?? "Unknown",
                        PlayedAt = match.PlayedAt
                    });
                }
            }

            return matchStats;
        }

        private async Task<List<object>> GetSeriesMatchStatsLoL(Series series)
        {
            // In LoL, each "match" is a single game; the UI should label them "Game 1", "Game 2", etc.
            var orderedMatches = series.Matches
                .OrderBy(m => m.PlayedAt)
                .ToList();

            var matchStats = new List<object>();

            for (int i = 0; i < orderedMatches.Count; i++)
            {
                var match = orderedMatches[i];
                if (match is not MatchLoL lolMatch)
                    continue;

                var mapId = i + 1;
                var mapName = $"Game {mapId}";

                var teamARounds = 0;
                var teamBRounds = 0;
                var winner = "Unknown";

                if (match.WinnerTeamId == series.TeamAId)
                {
                    teamARounds = 1;
                    winner = series.TeamA?.FullName ?? "Team A";
                }
                else if (match.WinnerTeamId == series.TeamBId)
                {
                    teamBRounds = 1;
                    winner = series.TeamB?.FullName ?? "Team B";
                }

                matchStats.Add(new
                {
                    MatchId = match.Id,
                    MapId = mapId,
                    MapName = mapName,
                    MapImage = "/images/default-map.png",
                    TeamARounds = teamARounds,
                    TeamBRounds = teamBRounds,
                    TotalRounds = 1,
                    Winner = winner,
                    PlayedAt = match.PlayedAt
                });
            }

            return matchStats;
        }

        private async Task<List<PlayerStatsViewModel>> GetSeriesPlayerStats(Series series, int? mapId = null)
        {
            var playerStats = new Dictionary<int, PlayerStatsViewModel>();

            foreach (var match in series.Matches)
            {
                // Filter by map if specified
                if (mapId.HasValue && match is MatchCS csMatch && csMatch.MapId != mapId.Value)
                    continue;

                foreach (var stat in match.PlayerStats)
                {
                    if (stat is PlayerStatistic_CS2 csStat)
                    {
                        var playerId = await GetPlayerIdFromUuid(csStat.PlayerUUID);
                        if (playerId.HasValue)
                        {
                            if (!playerStats.ContainsKey(playerId.Value))
                            {
                                var player = await _context.Players.FindAsync(playerId.Value);
                                
                                // Determine which team this player belongs to based on the match
                                string teamSlot = "Unknown";
                                if (match is MatchCS matchCs)
                                {
                                    // The stat.Team contains the FACEIT team ID (UUID)
                                    // We need to map this to our internal Team1/Team2 based on the match's team assignments
                                    
                                    // Get all unique FACEIT team IDs from this match's player stats
                                    var faceitTeamIds = matchCs.PlayerStats.Select(ps => ps.Team).Distinct().ToList();
                                    
                                    if (faceitTeamIds.Count >= 2)
                                    {
                                        var firstTeamId = faceitTeamIds[0];
                                        var secondTeamId = faceitTeamIds[1];
                                        
                                        // Determine which team this player belongs to
                                        if (stat.Team == firstTeamId)
                                        {
                                            // This player belongs to the first team
                                            // Check if this match's TeamA corresponds to series TeamA
                                            if (matchCs.TeamAId == series.TeamAId)
                                                teamSlot = "Team1";
                                            else
                                                teamSlot = "Team2";
                                        }
                                        else if (stat.Team == secondTeamId)
                                        {
                                            // This player belongs to the second team
                                            // Check if this match's TeamB corresponds to series TeamB
                                            if (matchCs.TeamBId == series.TeamBId)
                                                teamSlot = "Team2";
                                            else
                                                teamSlot = "Team1";
                                        }
                                    }
                                    
                                    // Fallback: determine team based on winner if we couldn't determine above
                                    if (teamSlot == "Unknown")
                                    {
                                        if (stat.IsWinner && match.WinnerTeamId == series.TeamAId)
                                            teamSlot = "Team1";
                                        else if (stat.IsWinner && match.WinnerTeamId == series.TeamBId)
                                            teamSlot = "Team2";
                                        else if (!stat.IsWinner && match.WinnerTeamId == series.TeamAId)
                                            teamSlot = "Team2";
                                        else if (!stat.IsWinner && match.WinnerTeamId == series.TeamBId)
                                            teamSlot = "Team1";
                                    }
                                }

                                playerStats[playerId.Value] = new PlayerStatsViewModel
                                {
                                    PlayerId = playerId.Value,
                                    PlayerName = player?.Nickname ?? "Unknown Player",
                                    Team = teamSlot,
                                    TotalKills = 0,
                                    TotalDeaths = 0,
                                    TotalAssists = 0,
                                    TotalDamage = 0,
                                    TotalRounds = 0,
                                    MapsPlayed = 0,
                                    IsWinner = false,
                                    HLTVRating = 0.0
                                };
                            }

                            var currentStats = playerStats[playerId.Value];
                            currentStats.TotalKills += csStat.Kills;
                            currentStats.TotalDeaths += csStat.Deaths;
                            currentStats.TotalAssists += csStat.Assists;
                            currentStats.TotalDamage += csStat.Damage;
                            currentStats.TotalRounds += csStat.RoundsPlayed;
                            currentStats.MapsPlayed += 1;
                            currentStats.IsWinner = csStat.IsWinner;
                        }
                    }
                }
            }

            // Calculate HLTV rating for each player
            foreach (var player in playerStats.Values)
            {
                if (player.TotalRounds > 0)
                {
                    double kd = player.TotalDeaths > 0 ? (double)player.TotalKills / player.TotalDeaths : player.TotalKills;
                    double kr = (double)player.TotalKills / player.TotalRounds;
                    player.HLTVRating = (0.0073 * player.TotalKills) + (0.3591 * kd) + (0.5329 * kr);
                }
            }

            return playerStats.Values.ToList();
        }

        private async Task<List<LoLPlayerStatsViewModel>> GetSeriesPlayerStatsLoL(Series series, int? mapId)
        {
            // LoL series scoreboard:
            // - If mapId is null => "All Maps": show numeric totals only, omit icons.
            // - If mapId is not null => show numeric totals for that single game + champion/item icons.
            var orderedMatches = series.Matches
                .OrderBy(m => m.PlayedAt)
                .ToList();

            var selectedMatches = new List<MatchLoL>();
            if (mapId.HasValue)
            {
                var idx = mapId.Value - 1;
                if (idx >= 0 && idx < orderedMatches.Count && orderedMatches[idx] is MatchLoL selectedLol)
                    selectedMatches.Add(selectedLol);
            }
            else
            {
                selectedMatches = orderedMatches.OfType<MatchLoL>().ToList();
            }

            if (!selectedMatches.Any())
                return new List<LoLPlayerStatsViewModel>();

            // Resolve internal player ids for all participant puuids involved in the selection.
            var uuids = selectedMatches
                .SelectMany(m => m.PlayerStats)
                .Where(ps => ps is PlayerStatistic_LoL)
                .Select(ps => ps.PlayerUUID)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .ToList();

            var uuidToPlayerId = await _context.GameProfiles
                .Where(gp => gp.Provider == "RIOT" && uuids.Contains(gp.UUID))
                .Select(gp => new { gp.UUID, gp.PlayerId })
                .ToListAsync();

            var uuidMap = uuidToPlayerId.ToDictionary(x => x.UUID, x => x.PlayerId);

            var playerStats = new Dictionary<int, LoLPlayerStatsViewModel>();

            // Aggregate stats match-by-match so team assignment can be based on match time.
            foreach (var match in selectedMatches)
            {
                var matchDate = match.PlayedAt;
                string? ddragonVersion = null;
                if (mapId.HasValue)
                    ddragonVersion = await _ddragonVersion.GetDDragonVersionAsync(match.GameVersion);

                var stats = match.PlayerStats.OfType<PlayerStatistic_LoL>().ToList();
                if (!stats.Any())
                    continue;

                var matchPlayerIds = stats
                    .Select(s => uuidMap.TryGetValue(s.PlayerUUID, out var pid) ? pid : (int?)null)
                    .Where(pid => pid.HasValue)
                    .Select(pid => pid.Value)
                    .Distinct()
                    .ToList();

                // Determine which internal team each player belonged to at the match timestamp.
                var transfers = await _context.PlayerTeamTransfers
                    .Where(t =>
                        matchPlayerIds.Contains(t.PlayerId) &&
                        t.Status == PlayerTeamStatus.Active &&
                        t.StartDate <= matchDate &&
                        (t.EndDate == null || t.EndDate >= matchDate) &&
                        (t.TeamId == series.TeamAId || t.TeamId == series.TeamBId))
                    .ToListAsync();

                var playerIdToTeamId = transfers
                    .GroupBy(t => t.PlayerId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(t => t.StartDate).First().TeamId);

                var playerIdToPositionId = transfers
                    .GroupBy(t => t.PlayerId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(t => t.StartDate).First().PositionId);

                foreach (var s in stats)
                {
                    if (!uuidMap.TryGetValue(s.PlayerUUID, out var playerId))
                        continue;

                    if (!playerIdToTeamId.TryGetValue(playerId, out var teamId) || teamId == null)
                        continue;

                    var teamSlot = teamId == series.TeamAId ? "Team1" : "Team2";
                    var isWinner = series.WinnerTeamId.HasValue && teamId == series.WinnerTeamId.Value;

                    if (!playerStats.TryGetValue(playerId, out var vm))
                    {
                        vm = new LoLPlayerStatsViewModel
                        {
                            PlayerId = playerId,
                            PlayerName = "Unknown",
                            Team = teamSlot,
                            Kills = 0,
                            Deaths = 0,
                            Assists = 0,
                            CreepScore = 0,
                            VisionScore = 0,
                            TotalDamageToChampions = 0,
                            IsWinner = isWinner,
                        };

                        // Player name resolution
                        var playerEntity = await _context.Players.FindAsync(playerId);
                        if (playerEntity != null)
                        {
                            var fullName = ($"{playerEntity.FirstName} {playerEntity.LastName}").Trim();
                            vm.PlayerName = !string.IsNullOrWhiteSpace(playerEntity.Nickname)
                                ? playerEntity.Nickname
                                : (!string.IsNullOrWhiteSpace(fullName) ? fullName : "Unknown");
                        }

                        // Icons only for map-specific mode
                        if (mapId.HasValue)
                        {
                            vm.ChampionName = s.ChampionName;
                            vm.ItemIds = new List<int> { s.Item0, s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6 };
                            vm.DDragonVersion = ddragonVersion;
                        }

                        playerStats[playerId] = vm;
                    }

                    vm.Kills += s.Kills ?? 0;
                    vm.Deaths += s.Deaths ?? 0;
                    vm.Assists += s.Assists ?? 0;
                    vm.CreepScore += s.CreepScore;
                    vm.VisionScore += s.VisionScore;
                    vm.TotalDamageToChampions += s.TotalDamageToChampions ?? 0;

                    // Keep winner/isWinner consistent with the series winner.
                    vm.IsWinner = isWinner;

                    if (playerIdToPositionId.TryGetValue(playerId, out var posId))
                        vm.LolPositionId = posId;
                }
            }

            int LolRoleOrder(int? p)
            {
                if (p is int pos && pos >= 9 && pos <= 13) return pos - 9;
                if (p.HasValue) return 20 + p.Value;
                return 200;
            }

            return playerStats.Values
                .OrderBy(v => v.Team)
                .ThenBy(v => LolRoleOrder(v.LolPositionId))
                .ThenBy(v => v.PlayerName)
                .ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetPlayerStatsByMap(int seriesId, int? mapId)
        {
            // Load base series data without CS-only map include (so LoL series won't break).
            var series = await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Tournament.Game)
                .Include(s => s.Matches)
                    .ThenInclude(m => m.PlayerStats)
                .FirstOrDefaultAsync(s => s.Id == seriesId);

            if (series == null) return NotFound();

            // For CS2, re-load with CS-only map include so existing CS logic stays intact.
            if (string.Equals(series.Tournament?.Game?.ShortName, "CS2", StringComparison.OrdinalIgnoreCase))
            {
                series = await _context.Series
                    .Include(s => s.TeamA)
                    .Include(s => s.TeamB)
                    .Include(s => s.Tournament.Game)
                    .Include(s => s.Matches)
                        .ThenInclude(m => m.PlayerStats)
                    .Include(s => s.Matches)
                        .ThenInclude(m => ((MatchCS)m).Map)
                    .FirstOrDefaultAsync(s => s.Id == seriesId);

                var playerStats = await GetSeriesPlayerStats(series, mapId);
                return PartialView("_PlayerStatsPartial", playerStats);
            }

            // LoL stats will be implemented in the next steps of the plan.
            var playerStatsLoL = await GetSeriesPlayerStatsLoL(series, mapId);
            return PartialView("_LoLPlayerStatsPartial", playerStatsLoL);
        }

        private async Task<int?> GetPlayerIdFromUuid(string uuid)
        {
            var gameProfile = await _context.GameProfiles
                .Include(gp => gp.Player)
                .FirstOrDefaultAsync(gp => gp.UUID == uuid && gp.Provider == "FACEIT");

            return gameProfile?.PlayerId;
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Create()
        {
            ViewData["TeamAId"] = new SelectList(_context.Teams, "Id", "FullName");
            ViewData["TeamBId"] = new SelectList(_context.Teams, "Id", "FullName");
            ViewData["GameId"] = new SelectList(_context.Games, "Id", "FullName");
            ViewData["TournamentId"] = new SelectList(_context.Tournaments, "Id", "FullName");
            return View();
        }

        // POST: Series/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Create([Bind("Id,Name,TeamAId,TeamBId,GameId,TournamentId,DatePlayed")] Series series)
        {
            if (ModelState.ErrorCount<=4)
            {
                _context.Add(series);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(series);
        }

        // GET: Series/Edit/5
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var series = await _context.Series
                .Include(s => s.Tournament)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null) return NotFound();

            await PopulateSeriesEditViewDataAsync(series);

            return View(series);
        }

        // POST: Series/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,TeamAId,TeamBId,GameId,TournamentId,DatePlayed")] Series series)
        {
            if (id != series.Id) return NotFound();

            if (ModelState.ErrorCount <= 4)
            {
                _context.Update(series);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateSeriesEditViewDataAsync(series);
            return View(series);
        }

        private async Task PopulateSeriesEditViewDataAsync(Series series)
        {
            var tournament = series.Tournament;
            if (tournament == null && series.TournamentId != 0)
            {
                tournament = await _context.Tournaments.AsNoTracking().FirstOrDefaultAsync(t => t.Id == series.TournamentId);
            }

            var gameId = tournament?.GameId ?? 0;

            var teamIds = await _context.TournamentTeams
                .Where(tt => tt.TournamentId == series.TournamentId)
                .Select(tt => tt.TeamId)
                .ToListAsync();

            var teams = await _context.Teams
                .Where(t => teamIds.Contains(t.Id))
                .OrderBy(t => t.FullName)
                .AsNoTracking()
                .ToListAsync();

            ViewData["TeamAId"] = BuildSeriesEditTeamSelectList(teams, series.TeamAId);
            ViewData["TeamBId"] = BuildSeriesEditTeamSelectList(teams, series.TeamBId);

            var games = await _context.Games.AsNoTracking().OrderBy(g => g.FullName).ToListAsync();
            var tournaments = await _context.Tournaments.AsNoTracking().OrderBy(t => t.FullName).ToListAsync();
            ViewData["GameId"] = new SelectList(games, "Id", "FullName", gameId);
            ViewData["TournamentId"] = new SelectList(tournaments, "Id", "FullName", series.TournamentId);
        }

        private static SelectList BuildSeriesEditTeamSelectList(IReadOnlyList<Team> teams, int? selectedTeamId)
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "TBD" }
            };
            foreach (var t in teams)
            {
                items.Add(new SelectListItem { Value = t.Id.ToString(), Text = t.FullName });
            }

            var selected = selectedTeamId.HasValue ? selectedTeamId.Value.ToString() : "";
            return new SelectList(items, "Value", "Text", selected);
        }

        // GET: Series/Delete/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var series = await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Tournament.Game)
                .Include(s => s.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (series == null) return NotFound();

            return View(series);
        }

        // POST: Series/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var series = await _context.Series.FindAsync(id);
            if (series != null)
            {
                _context.Series.Remove(series);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
