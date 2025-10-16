using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Balkana.Models.Series;

namespace Balkana.Controllers
{
    public class SeriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeriesController(ApplicationDbContext context)
        {
            _context = context;
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

            var series = await _context.Series
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

            if (series == null) return NotFound();

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
                var matchStats = await GetSeriesMatchStats(series);
                var playerStats = await GetSeriesPlayerStats(series);

                ViewData["MatchStats"] = matchStats;
                ViewData["PlayerStats"] = playerStats;
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

        [HttpGet]
        public async Task<IActionResult> GetPlayerStatsByMap(int seriesId, int? mapId)
        {
            var series = await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Matches)
                    .ThenInclude(m => m.PlayerStats)
                .Include(s => s.Matches)
                    .ThenInclude(m => ((MatchCS)m).Map)
                .FirstOrDefaultAsync(s => s.Id == seriesId);

            if (series == null) return NotFound();

            var playerStats = await GetSeriesPlayerStats(series, mapId);
            
            return PartialView("_PlayerStatsPartial", playerStats);
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

            ViewData["TeamAId"] = new SelectList(_context.Teams, "Id", "FullName", series.TeamAId);
            ViewData["TeamBId"] = new SelectList(_context.Teams, "Id", "FullName", series.TeamBId);
            ViewData["GameId"] = new SelectList(_context.Games, "Id", "FullName", series.Tournament.GameId);
            ViewData["TournamentId"] = new SelectList(_context.Tournaments, "Id", "FullName", series.TournamentId);

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
            return View(series);
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
