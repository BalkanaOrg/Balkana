using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Stats;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Stats
{
    public class StatsService : IStatsService
    {
        private readonly ApplicationDbContext _context;

        public StatsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PlayerStatsResponseModel>> GetPlayerStatsAsync(StatsRequestModel request)
        {
            // First get the player's game profiles to find UUIDs
            var gameProfiles = await _context.GameProfiles
                .Where(gp => gp.PlayerId == request.PlayerId)
                .ToListAsync();

            if (!gameProfiles.Any())
                return new List<PlayerStatsResponseModel>();

            var playerUUIDs = gameProfiles.Select(gp => gp.UUID).ToList();

            var query = _context.PlayerStats
                .Include(ps => ps.Match)
                .ThenInclude(m => m.Series)
                .ThenInclude(s => s.Tournament)
                .ThenInclude(t => t.Game)
                .Where(ps => playerUUIDs.Contains(ps.PlayerUUID));

            // Apply filters
            if (request.GameId.HasValue)
            {
                query = query.Where(ps => ps.Match.Series.Tournament.GameId == request.GameId.Value);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(ps => ps.Match.PlayedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(ps => ps.Match.PlayedAt <= request.EndDate.Value);
            }

            var stats = await query.ToListAsync();

            // Get player information
            var player = await _context.Players.FindAsync(request.PlayerId.Value);
            if (player == null)
                return new List<PlayerStatsResponseModel>();

            // Group by game type and aggregate
            var result = new List<PlayerStatsResponseModel>();

            // CS2 Stats
            var cs2Stats = stats.OfType<PlayerStatistic_CS2>().ToList();
            if (cs2Stats.Any())
            {
                var cs2Model = new PlayerStatsResponseModel
                {
                    PlayerId = request.PlayerId.Value,
                    PlayerName = player.FirstName + " " + player.LastName,
                    PlayerNickname = player.Nickname,
                    Provider = "MANUAL", // Default for manual entries
                    PlayerUUID = cs2Stats.First().PlayerUUID,
                    CS2Stats = AggregateCS2Stats(cs2Stats),
                    TotalMatches = cs2Stats.Count,
                    FirstMatchDate = cs2Stats.Min(s => s.Match.PlayedAt),
                    LastMatchDate = cs2Stats.Max(s => s.Match.PlayedAt),
                    TeamsPlayedFor = cs2Stats
                        .Where(s => s.Match.Series.Tournament != null)
                        .Select(s => s.Match.Series.Tournament.FullName)
                        .Distinct()
                        .ToList()
                };
                result.Add(cs2Model);
            }

            // LoL Stats
            var lolStats = stats.OfType<PlayerStatistic_LoL>().ToList();
            if (lolStats.Any())
            {
                var lolModel = new PlayerStatsResponseModel
                {
                    PlayerId = request.PlayerId.Value,
                    PlayerName = player.FirstName + " " + player.LastName,
                    PlayerNickname = player.Nickname,
                    Provider = "RIOT",
                    PlayerUUID = lolStats.First().PlayerUUID,
                    LoLStats = AggregateLoLStats(lolStats),
                    TotalMatches = lolStats.Count,
                    FirstMatchDate = lolStats.Min(s => s.Match.PlayedAt),
                    LastMatchDate = lolStats.Max(s => s.Match.PlayedAt),
                    TeamsPlayedFor = lolStats
                        .Where(s => s.Match.Series.Tournament != null)
                        .Select(s => s.Match.Series.Tournament.FullName)
                        .Distinct()
                        .ToList()
                };
                result.Add(lolModel);
            }

            return result;
        }

        public async Task<List<PlayerStatsResponseModel>> GetTeamStatsAsync(StatsRequestModel request, TeamRosterType rosterType = TeamRosterType.CurrentRoster)
        {
            // Get team players based on roster type
            var teamPlayersQuery = _context.PlayerTeamTransfers
                .Include(ptt => ptt.Player)
                .Where(ptt => ptt.TeamId == request.TeamId);

            if (rosterType == TeamRosterType.CurrentRoster)
            {
                // Only active, substitute, and emergency substitute players
                teamPlayersQuery = teamPlayersQuery.Where(ptt => 
                    ptt.Status == PlayerTeamStatus.Active ||
                    ptt.Status == PlayerTeamStatus.Substitute ||
                    ptt.Status == PlayerTeamStatus.EmergencySubstitute);
            }

            var teamPlayers = await teamPlayersQuery
                .Select(ptt => ptt.PlayerId)
                .ToListAsync();

            if (!teamPlayers.Any())
                return new List<PlayerStatsResponseModel>();

            // Get stats for all team players
            var allPlayerStats = new List<PlayerStatsResponseModel>();
            
            foreach (var playerId in teamPlayers)
            {
                var playerRequest = new StatsRequestModel
                {
                    RequestType = StatsRequestType.Player,
                    PlayerId = playerId,
                    Provider = request.Provider,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    GameId = request.GameId
                };

                var playerStats = await GetPlayerStatsAsync(playerRequest);
                allPlayerStats.AddRange(playerStats);
            }

            return allPlayerStats;
        }

        public async Task<List<PlayerStatsResponseModel>> GetSeriesStatsAsync(StatsRequestModel request)
        {
            var query = _context.PlayerStats
                .Include(ps => ps.Match)
                .ThenInclude(m => m.Series)
                .ThenInclude(s => s.Tournament)
                .ThenInclude(t => t.Game)
                .Where(ps => ps.Match.SeriesId == request.SeriesId);

            // Apply filters
            if (request.GameId.HasValue)
            {
                query = query.Where(ps => ps.Match.Series.Tournament != null && ps.Match.Series.Tournament.GameId == request.GameId.Value);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(ps => ps.Match.PlayedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(ps => ps.Match.PlayedAt <= request.EndDate.Value);
            }

            var stats = await query.ToListAsync();

            // Group by player UUID and aggregate
            var playerGroups = stats.GroupBy(s => s.PlayerUUID);
            var result = new List<PlayerStatsResponseModel>();

            foreach (var group in playerGroups)
            {
                var playerStats = group.ToList();
                var cs2Stats = playerStats.OfType<PlayerStatistic_CS2>().ToList();
                var lolStats = playerStats.OfType<PlayerStatistic_LoL>().ToList();

                // Get player information from GameProfile
                var gameProfile = await _context.GameProfiles
                    .Include(gp => gp.Player)
                    .FirstOrDefaultAsync(gp => gp.UUID == group.Key);

                if (gameProfile == null) continue;

                if (cs2Stats.Any())
                {
                    var cs2Model = new PlayerStatsResponseModel
                    {
                        PlayerId = gameProfile.PlayerId,
                        PlayerName = gameProfile.Player.FirstName + " " + gameProfile.Player.LastName,
                        PlayerNickname = gameProfile.Player.Nickname,
                        Provider = "MANUAL",
                        PlayerUUID = group.Key,
                        CS2Stats = AggregateCS2Stats(cs2Stats),
                        TotalMatches = cs2Stats.Count,
                        FirstMatchDate = cs2Stats.Min(s => s.Match.PlayedAt),
                        LastMatchDate = cs2Stats.Max(s => s.Match.PlayedAt),
                        TeamsPlayedFor = cs2Stats
                            .Where(s => s.Match.Series.Tournament != null)
                            .Select(s => s.Match.Series.Tournament.ShortName ?? s.Match.Series.Tournament.FullName)
                            .Distinct()
                            .ToList()
                    };
                    result.Add(cs2Model);
                }

                if (lolStats.Any())
                {
                    var lolModel = new PlayerStatsResponseModel
                    {
                        PlayerId = gameProfile.PlayerId,
                        PlayerName = gameProfile.Player.FirstName + " " + gameProfile.Player.LastName,
                        PlayerNickname = gameProfile.Player.Nickname,
                        Provider = "RIOT",
                        PlayerUUID = group.Key,
                        LoLStats = AggregateLoLStats(lolStats),
                        TotalMatches = lolStats.Count,
                        FirstMatchDate = lolStats.Min(s => s.Match.PlayedAt),
                        LastMatchDate = lolStats.Max(s => s.Match.PlayedAt),
                        TeamsPlayedFor = lolStats
                            .Where(s => s.Match.Series.Tournament != null)
                            .Select(s => s.Match.Series.Tournament.FullName)
                            .Distinct()
                            .ToList()
                    };
                    result.Add(lolModel);
                }
            }

            return result;
        }

        public async Task<List<PlayerStatsResponseModel>> GetTournamentStatsAsync(StatsRequestModel request)
        {
            var query = _context.PlayerStats
                .Include(ps => ps.Match)
                .ThenInclude(m => m.Series)
                .ThenInclude(s => s.Tournament)
                .Where(ps => ps.Match.Series.TournamentId == request.TournamentId);

            // Apply filters
            if (request.GameId.HasValue)
            {
                query = query.Where(ps => ps.Match.Series.Tournament.GameId == request.GameId.Value);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(ps => ps.Match.PlayedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(ps => ps.Match.PlayedAt <= request.EndDate.Value);
            }

            var stats = await query.ToListAsync();

            // Group by player UUID and aggregate
            var playerGroups = stats.GroupBy(s => s.PlayerUUID);
            var result = new List<PlayerStatsResponseModel>();

            foreach (var group in playerGroups)
            {
                var playerStats = group.ToList();
                var cs2Stats = playerStats.OfType<PlayerStatistic_CS2>().ToList();
                var lolStats = playerStats.OfType<PlayerStatistic_LoL>().ToList();

                // Get player information from GameProfile
                var gameProfile = await _context.GameProfiles
                    .Include(gp => gp.Player)
                    .FirstOrDefaultAsync(gp => gp.UUID == group.Key);

                if (gameProfile == null) continue;

                if (cs2Stats.Any())
                {
                    var cs2Model = new PlayerStatsResponseModel
                    {
                        PlayerId = gameProfile.PlayerId,
                        PlayerName = gameProfile.Player.FirstName + " " + gameProfile.Player.LastName,
                        PlayerNickname = gameProfile.Player.Nickname,
                        Provider = "MANUAL",
                        PlayerUUID = group.Key,
                        CS2Stats = AggregateCS2Stats(cs2Stats),
                        TotalMatches = cs2Stats.Count,
                        FirstMatchDate = cs2Stats.Min(s => s.Match.PlayedAt),
                        LastMatchDate = cs2Stats.Max(s => s.Match.PlayedAt),
                        TeamsPlayedFor = cs2Stats
                            .Where(s => s.Match.Series.Tournament != null)
                            .Select(s => s.Match.Series.Tournament.FullName)
                            .Distinct()
                            .ToList()
                    };
                    result.Add(cs2Model);
                }

                if (lolStats.Any())
                {
                    var lolModel = new PlayerStatsResponseModel
                    {
                        PlayerId = gameProfile.PlayerId,
                        PlayerName = gameProfile.Player.FirstName + " " + gameProfile.Player.LastName,
                        PlayerNickname = gameProfile.Player.Nickname,
                        Provider = "RIOT",
                        PlayerUUID = group.Key,
                        LoLStats = AggregateLoLStats(lolStats),
                        TotalMatches = lolStats.Count,
                        FirstMatchDate = lolStats.Min(s => s.Match.PlayedAt),
                        LastMatchDate = lolStats.Max(s => s.Match.PlayedAt),
                        TeamsPlayedFor = lolStats
                            .Where(s => s.Match.Series.Tournament != null)
                            .Select(s => s.Match.Series.Tournament.FullName)
                            .Distinct()
                            .ToList()
                    };
                    result.Add(lolModel);
                }
            }

            return result;
        }

        public async Task<TeamAggregatedStats> GetTeamAggregatedStatsAsync(StatsRequestModel request, TeamRosterType rosterType = TeamRosterType.CurrentRoster)
        {
            var playerStats = await GetTeamStatsAsync(request, rosterType);
            
            var teamStats = new TeamAggregatedStats();

            // Aggregate CS2 stats
            var cs2Stats = playerStats.Where(p => p.CS2Stats != null).Select(p => p.CS2Stats).ToList();
            if (cs2Stats.Any())
            {
                teamStats.CS2Stats = new CS2TeamStatsModel
                {
                    TotalKills = cs2Stats.Sum(s => s.TotalKills),
                    TotalDeaths = cs2Stats.Sum(s => s.TotalDeaths),
                    TotalAssists = cs2Stats.Sum(s => s.TotalAssists),
                    TotalDamage = cs2Stats.Sum(s => s.TotalDamage),
                    TotalRounds = cs2Stats.Sum(s => s.TotalRounds),
                    TeamKDRatio = cs2Stats.Sum(s => s.TotalKills) / (double)Math.Max(cs2Stats.Sum(s => s.TotalDeaths), 1),
                    TeamADR = cs2Stats.Sum(s => s.TotalDamage) / (double)Math.Max(cs2Stats.Sum(s => s.TotalRounds), 1),
                    TeamHLTVRating = cs2Stats.Average(s => s.HLTVRating),
                    TeamKAST = cs2Stats.Average(s => s.KAST),
                    TeamHeadshots = cs2Stats.Sum(s => s.Headshots),
                    TeamFirstKills = cs2Stats.Sum(s => s.FirstKills),
                    TeamFirstDeaths = cs2Stats.Sum(s => s.FirstDeaths),
                    Team_5k = cs2Stats.Sum(s => s._5k),
                    Team_4k = cs2Stats.Sum(s => s._4k),
                    Team_3k = cs2Stats.Sum(s => s._3k),
                    Team_2k = cs2Stats.Sum(s => s._2k),
                    Team_1k = cs2Stats.Sum(s => s._1k),
                    Team_1v1 = cs2Stats.Sum(s => s._1v1),
                    Team_1v2 = cs2Stats.Sum(s => s._1v2),
                    Team_1v3 = cs2Stats.Sum(s => s._1v3),
                    Team_1v4 = cs2Stats.Sum(s => s._1v4),
                    Team_1v5 = cs2Stats.Sum(s => s._1v5),
                    TeamSniperKills = cs2Stats.Sum(s => s.SniperKills),
                    TeamPistolKills = cs2Stats.Sum(s => s.PistolKills),
                    TeamKnifeKills = cs2Stats.Sum(s => s.KnifeKills),
                    TeamWallbangKills = cs2Stats.Sum(s => s.WallbangKills),
                    TeamCollateralKills = cs2Stats.Sum(s => s.CollateralKills),
                    TeamNoScopeKills = cs2Stats.Sum(s => s.NoScopeKills),
                    TeamFlashes = cs2Stats.Sum(s => s.Flashes),
                    TeamUtilityUsage = cs2Stats.Sum(s => s.UtilityUsage)
                };
            }

            // Aggregate LoL stats
            var lolStats = playerStats.Where(p => p.LoLStats != null).Select(p => p.LoLStats).ToList();
            if (lolStats.Any())
            {
                teamStats.LoLStats = new LoLTeamStatsModel
                {
                    TotalKills = lolStats.Sum(s => s.TotalKills),
                    TotalDeaths = lolStats.Sum(s => s.TotalDeaths),
                    TotalAssists = lolStats.Sum(s => s.TotalAssists),
                    TotalGoldEarned = lolStats.Sum(s => s.TotalGoldEarned),
                    TotalCreepScore = lolStats.Sum(s => s.TotalCreepScore),
                    TotalVisionScore = lolStats.Sum(s => s.TotalVisionScore),
                    TeamKDRatio = lolStats.Sum(s => s.TotalKills) / (double)Math.Max(lolStats.Sum(s => s.TotalDeaths), 1),
                    TeamKPARatio = (lolStats.Sum(s => s.TotalKills) + lolStats.Sum(s => s.TotalAssists)) / (double)Math.Max(lolStats.Sum(s => s.TotalDeaths), 1),
                    TeamGoldPerMinute = lolStats.Average(s => s.GoldPerMinute),
                    TeamCSPerMinute = lolStats.Average(s => s.CSPerMinute),
                    TeamDamageToChampions = lolStats.Sum(s => s.TotalDamageToChampions),
                    TeamDamageToObjectives = lolStats.Sum(s => s.TotalDamageToObjectives)
                };
            }

            return teamStats;
        }

        private CS2StatsModel AggregateCS2Stats(List<PlayerStatistic_CS2> stats)
        {
            if (!stats.Any()) return new CS2StatsModel();

            var totalKills = stats.Sum(s => s.Kills);
            var totalDeaths = stats.Sum(s => s.Deaths);
            var totalAssists = stats.Sum(s => s.Assists);
            var totalDamage = stats.Sum(s => s.Damage);
            var totalRounds = stats.Sum(s => s.RoundsPlayed);

            return new CS2StatsModel
            {
                TotalKills = totalKills,
                TotalDeaths = totalDeaths,
                TotalAssists = totalAssists,
                TotalDamage = totalDamage,
                TotalRounds = totalRounds,
                KDRatio = totalKills / (double)Math.Max(totalDeaths, 1),
                ADR = totalDamage / (double)Math.Max(totalRounds, 1),
                HLTVRating = stats.Average(s => s.HLTV1),
                KAST = (int)stats.Average(s => s.KAST),
                Headshots = stats.Sum(s => s.HSkills),
                FirstKills = stats.Sum(s => s.FK),
                FirstDeaths = stats.Sum(s => s.FD),
                MultiKills = stats.Sum(s => s._2k + s._3k + s._4k + s._5k),
                Clutches = stats.Sum(s => s._1v1 + s._1v2 + s._1v3 + s._1v4 + s._1v5),
                _5k = stats.Sum(s => s._5k),
                _4k = stats.Sum(s => s._4k),
                _3k = stats.Sum(s => s._3k),
                _2k = stats.Sum(s => s._2k),
                _1k = stats.Sum(s => s._1k),
                _1v1 = stats.Sum(s => s._1v1),
                _1v2 = stats.Sum(s => s._1v2),
                _1v3 = stats.Sum(s => s._1v3),
                _1v4 = stats.Sum(s => s._1v4),
                _1v5 = stats.Sum(s => s._1v5),
                SniperKills = stats.Sum(s => s.SniperKills),
                PistolKills = stats.Sum(s => s.PistolKills),
                KnifeKills = stats.Sum(s => s.KnifeKills),
                WallbangKills = stats.Sum(s => s.WallbangKills),
                CollateralKills = stats.Sum(s => s.CollateralKills),
                NoScopeKills = stats.Sum(s => s.NoScopeKills),
                Flashes = stats.Sum(s => s.Flashes),
                UtilityUsage = stats.Sum(s => s.UtilityUsage)
            };
        }

        private LoLStatsModel AggregateLoLStats(List<PlayerStatistic_LoL> stats)
        {
            if (!stats.Any()) return new LoLStatsModel();

            var totalKills = stats.Sum(s => s.Kills ?? 0);
            var totalDeaths = stats.Sum(s => s.Deaths ?? 0);
            var totalAssists = stats.Sum(s => s.Assists ?? 0);
            var totalGoldEarned = stats.Sum(s => s.GoldEarned);
            var totalCreepScore = stats.Sum(s => s.CreepScore);
            var totalVisionScore = stats.Sum(s => s.VisionScore);

            return new LoLStatsModel
            {
                TotalKills = totalKills,
                TotalDeaths = totalDeaths,
                TotalAssists = totalAssists,
                TotalGoldEarned = totalGoldEarned,
                TotalCreepScore = totalCreepScore,
                TotalVisionScore = totalVisionScore,
                KDRatio = totalKills / (double)Math.Max(totalDeaths, 1),
                KPARatio = (totalKills + totalAssists) / (double)Math.Max(totalDeaths, 1),
                GoldPerMinute = totalGoldEarned / (double)Math.Max(stats.Count * 30, 1), // Assuming 30 min average game
                CSPerMinute = totalCreepScore / (double)Math.Max(stats.Count * 30, 1),
                TotalDamageToChampions = stats.Sum(s => s.TotalDamageToChampions ?? 0),
                TotalDamageToObjectives = stats.Sum(s => s.TotalDamageToObjectives ?? 0)
            };
        }
    }
}