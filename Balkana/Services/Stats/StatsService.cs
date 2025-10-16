using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Stats;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Stats
{
    public interface IStatsService
    {
        Task<List<PlayerStatsResponseModel>> GetPlayerStatsAsync(StatsRequestModel request);
        Task<List<PlayerStatsResponseModel>> GetTeamStatsAsync(StatsRequestModel request);
        Task<List<PlayerStatsResponseModel>> GetSeriesStatsAsync(StatsRequestModel request);
        Task<List<PlayerStatsResponseModel>> GetTournamentStatsAsync(StatsRequestModel request);
    }

    public class StatsService : IStatsService
    {
        private readonly ApplicationDbContext _context;

        public StatsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PlayerStatsResponseModel>> GetPlayerStatsAsync(StatsRequestModel request)
        {
            if (!request.PlayerId.HasValue)
                throw new ArgumentException("PlayerId is required for player stats");

            var player = await _context.Players
                .Include(p => p.GameProfiles)
                .FirstOrDefaultAsync(p => p.Id == request.PlayerId.Value);

            if (player == null)
                return new List<PlayerStatsResponseModel>();

            var result = new List<PlayerStatsResponseModel>();

            // Get stats for each provider
            foreach (var profile in player.GameProfiles)
            {
                if (!string.IsNullOrEmpty(request.Provider) && profile.Provider != request.Provider)
                    continue;

                var playerStats = await GetPlayerStatsForProvider(profile, request);
                if (playerStats != null)
                {
                    result.Add(playerStats);
                }
            }

            return result;
        }

        public async Task<List<PlayerStatsResponseModel>> GetTeamStatsAsync(StatsRequestModel request)
        {
            if (!request.TeamId.HasValue)
                throw new ArgumentException("TeamId is required for team stats");

            // Get all players who have played for this team
            var teamTransfers = await _context.PlayerTeamTransfers
                .Include(t => t.Player)
                    .ThenInclude(p => p.GameProfiles)
                .Where(t => t.TeamId == request.TeamId.Value)
                .ToListAsync();

            var result = new List<PlayerStatsResponseModel>();

            foreach (var transfer in teamTransfers)
            {
                var player = transfer.Player;
                
                // Filter by date range if specified
                var startDate = request.StartDate ?? transfer.StartDate;
                var endDate = request.EndDate ?? transfer.EndDate;

                foreach (var profile in player.GameProfiles)
                {
                    if (!string.IsNullOrEmpty(request.Provider) && profile.Provider != request.Provider)
                        continue;

                    var playerStats = await GetPlayerStatsForProvider(profile, request, startDate, endDate);
                    if (playerStats != null)
                    {
                        result.Add(playerStats);
                    }
                }
            }

            return result;
        }

        public async Task<List<PlayerStatsResponseModel>> GetSeriesStatsAsync(StatsRequestModel request)
        {
            if (!request.SeriesId.HasValue)
                throw new ArgumentException("SeriesId is required for series stats");

            // Get all matches for this series
            var matches = await _context.Matches
                .Where(m => m.SeriesId == request.SeriesId.Value)
                .Select(m => m.Id)
                .ToListAsync();

            return await GetPlayerStatsForMatches(matches, request);
        }

        public async Task<List<PlayerStatsResponseModel>> GetTournamentStatsAsync(StatsRequestModel request)
        {
            if (!request.TournamentId.HasValue)
                throw new ArgumentException("TournamentId is required for tournament stats");

            // Get all series for this tournament
            var seriesIds = await _context.Series
                .Where(s => s.TournamentId == request.TournamentId.Value)
                .Select(s => s.Id)
                .ToListAsync();

            // Get all matches for these series
            var matches = await _context.Matches
                .Where(m => seriesIds.Contains(m.SeriesId))
                .Select(m => m.Id)
                .ToListAsync();

            return await GetPlayerStatsForMatches(matches, request);
        }

        private async Task<PlayerStatsResponseModel?> GetPlayerStatsForProvider(
            GameProfile profile, 
            StatsRequestModel request, 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            var player = await _context.Players.FindAsync(profile.PlayerId);
            if (player == null) return null;

            // Get player statistics for this provider
            var playerStats = await _context.PlayerStats
                .Include(ps => ps.Match)
                    .ThenInclude(m => m.Series)
                .Where(ps => ps.PlayerUUID == profile.UUID && ps.Source == profile.Provider)
                .Where(ps => !request.StartDate.HasValue || ps.Match.PlayedAt >= request.StartDate.Value)
                .Where(ps => !request.EndDate.HasValue || ps.Match.PlayedAt <= request.EndDate.Value)
                .Where(ps => !startDate.HasValue || ps.Match.PlayedAt >= startDate.Value)
                .Where(ps => !endDate.HasValue || ps.Match.PlayedAt <= endDate.Value)
                .Where(ps => !request.GameId.HasValue || ps.Match.Series.Tournament.GameId == request.GameId.Value)
                .ToListAsync();

            if (!playerStats.Any()) return null;

            var result = new PlayerStatsResponseModel
            {
                PlayerId = player.Id,
                PlayerName = $"{player.FirstName} {player.LastName}",
                PlayerNickname = player.Nickname,
                Provider = profile.Provider,
                PlayerUUID = profile.UUID,
                TotalMatches = playerStats.Count,
                FirstMatchDate = playerStats.Min(ps => ps.Match.PlayedAt),
                LastMatchDate = playerStats.Max(ps => ps.Match.PlayedAt)
            };

            // Get teams played for
            result.TeamsPlayedFor = await GetTeamsPlayedFor(player.Id, startDate, endDate);

            // Process game-specific stats
            if (profile.Provider == "FACEIT" || profile.Provider == "MANUAL")
            {
                result.CS2Stats = await ProcessCS2Stats(playerStats);
            }
            else if (profile.Provider == "RIOT")
            {
                result.LoLStats = await ProcessLoLStats(playerStats);
            }

            return result;
        }

        private async Task<List<PlayerStatsResponseModel>> GetPlayerStatsForMatches(
            List<int> matchIds, 
            StatsRequestModel request)
        {
            if (!matchIds.Any()) return new List<PlayerStatsResponseModel>();

            // Get all player stats for these matches
            var playerStats = await _context.PlayerStats
                .Include(ps => ps.Match)
                    .ThenInclude(m => m.Series)
                .Where(ps => matchIds.Contains(ps.MatchId))
                .Where(ps => !request.StartDate.HasValue || ps.Match.PlayedAt >= request.StartDate.Value)
                .Where(ps => !request.EndDate.HasValue || ps.Match.PlayedAt <= request.EndDate.Value)
                .Where(ps => !request.GameId.HasValue || ps.Match.Series.Tournament.GameId == request.GameId.Value)
                .ToListAsync();

            // Group by player UUID and provider
            var groupedStats = playerStats
                .GroupBy(ps => new { ps.PlayerUUID, ps.Source })
                .ToList();

            var result = new List<PlayerStatsResponseModel>();

            foreach (var group in groupedStats)
            {
                var playerId = await GetPlayerIdFromUuid(group.Key.PlayerUUID, group.Key.Source);
                if (!playerId.HasValue) continue;

                var player = await _context.Players.FindAsync(playerId.Value);
                if (player == null) continue;

                var playerStatsList = group.ToList();

                var playerStatsResponse = new PlayerStatsResponseModel
                {
                    PlayerId = player.Id,
                    PlayerName = $"{player.FirstName} {player.LastName}",
                    PlayerNickname = player.Nickname,
                    Provider = group.Key.Source,
                    PlayerUUID = group.Key.PlayerUUID,
                    TotalMatches = playerStatsList.Count,
                    FirstMatchDate = playerStatsList.Min(ps => ps.Match.PlayedAt),
                    LastMatchDate = playerStatsList.Max(ps => ps.Match.PlayedAt)
                };

                // Get teams played for
                playerStatsResponse.TeamsPlayedFor = await GetTeamsPlayedFor(player.Id);

                // Process game-specific stats
                if (group.Key.Source == "FACEIT" || group.Key.Source == "MANUAL")
                {
                    playerStatsResponse.CS2Stats = await ProcessCS2Stats(playerStatsList);
                }
                else if (group.Key.Source == "RIOT")
                {
                    playerStatsResponse.LoLStats = await ProcessLoLStats(playerStatsList);
                }

                result.Add(playerStatsResponse);
            }

            return result;
        }

        private async Task<CS2StatsModel> ProcessCS2Stats(List<PlayerStatistic> playerStats)
        {
            var cs2Stats = playerStats.OfType<PlayerStatistic_CS2>().ToList();
            
            if (!cs2Stats.Any()) return new CS2StatsModel();

            var result = new CS2StatsModel
            {
                TotalKills = cs2Stats.Sum(s => s.Kills),
                TotalDeaths = cs2Stats.Sum(s => s.Deaths),
                TotalAssists = cs2Stats.Sum(s => s.Assists),
                TotalDamage = cs2Stats.Sum(s => s.Damage),
                TotalRounds = cs2Stats.Sum(s => s.RoundsPlayed),
                Headshots = cs2Stats.Sum(s => s.HSkills),
                FirstKills = cs2Stats.Sum(s => s.FK),
                FirstDeaths = cs2Stats.Sum(s => s.FD),
                MultiKills = cs2Stats.Sum(s => s._2k + s._3k + s._4k + s._5k),
                Clutches = cs2Stats.Sum(s => s._1v1 + s._1v2 + s._1v3 + s._1v4 + s._1v5),
                SniperKills = cs2Stats.Sum(s => s.SniperKills),
                PistolKills = cs2Stats.Sum(s => s.PistolKills),
                KnifeKills = cs2Stats.Sum(s => s.KnifeKills),
                Flashes = cs2Stats.Sum(s => s.Flashes),
                UtilityUsage = cs2Stats.Sum(s => s.UtilityUsage)
            };

            // Calculate derived stats
            result.KDRatio = result.TotalDeaths > 0 ? (double)result.TotalKills / result.TotalDeaths : result.TotalKills;
            result.ADR = result.TotalRounds > 0 ? (double)result.TotalDamage / result.TotalRounds : 0;
            result.KAST = cs2Stats.Average(s => s.KAST);
            
            // Calculate HLTV rating
            var avgKills = cs2Stats.Average(s => s.Kills);
            var avgKD = cs2Stats.Average(s => s.Deaths > 0 ? (double)s.Kills / s.Deaths : s.Kills);
            var avgKR = cs2Stats.Average(s => s.RoundsPlayed > 0 ? (double)s.Kills / s.RoundsPlayed : 0);
            result.HLTVRating = (0.0073 * avgKills) + (0.3591 * avgKD) + (0.5329 * avgKR);

            // Get map stats
            result.MapStats = await GetMapStats(cs2Stats);

            return result;
        }

        private async Task<LoLStatsModel> ProcessLoLStats(List<PlayerStatistic> playerStats)
        {
            var lolStats = playerStats.OfType<PlayerStatistic_LoL>().ToList();
            
            if (!lolStats.Any()) return new LoLStatsModel();

            var result = new LoLStatsModel
            {
                TotalKills = lolStats.Sum(s => s.Kills ?? 0),
                TotalDeaths = lolStats.Sum(s => s.Deaths ?? 0),
                TotalAssists = lolStats.Sum(s => s.Assists ?? 0),
                TotalGoldEarned = lolStats.Sum(s => s.GoldEarned),
                TotalCreepScore = lolStats.Sum(s => s.CreepScore),
                TotalVisionScore = lolStats.Sum(s => s.VisionScore),
                TotalDamageToChampions = lolStats.Sum(s => s.TotalDamageToChampions ?? 0),
                TotalDamageToObjectives = lolStats.Sum(s => s.TotalDamageToObjectives ?? 0)
            };

            // Calculate derived stats
            result.KDRatio = result.TotalDeaths > 0 ? (double)result.TotalKills / result.TotalDeaths : result.TotalKills;
            result.KPARatio = result.TotalDeaths > 0 ? (double)(result.TotalKills + result.TotalAssists) / result.TotalDeaths : (result.TotalKills + result.TotalAssists);
            
            var totalMinutes = lolStats.Count * 30; // Assuming average 30 minutes per game
            result.GoldPerMinute = totalMinutes > 0 ? (double)result.TotalGoldEarned / totalMinutes : 0;
            result.CSPerMinute = totalMinutes > 0 ? (double)result.TotalCreepScore / totalMinutes : 0;

            // Get champion and lane stats
            result.ChampionStats = await GetChampionStats(lolStats);
            result.LaneStats = await GetLaneStats(lolStats);

            return result;
        }

        private async Task<List<MapStatsModel>> GetMapStats(List<PlayerStatistic_CS2> cs2Stats)
        {
            var mapStats = cs2Stats
                .GroupBy(s => s.Match)
                .Select(g => new
                {
                    MapId = ((MatchCS)g.Key).MapId ?? 0,
                    Match = g.Key,
                    Stats = g.ToList()
                })
                .GroupBy(x => x.MapId)
                .Select(g => new MapStatsModel
                {
                    MapId = g.Key,
                    MapName = g.First().Match is MatchCS csMatch ? csMatch.Map?.Name ?? "Unknown" : "Unknown",
                    MatchesPlayed = g.Count(),
                    Wins = g.Count(x => x.Stats.Any(s => s.IsWinner)),
                    AverageRating = g.Average(x => x.Stats.Average(s => s.HLTV1)),
                    AverageADR = g.Average(x => x.Stats.Average(s => s.RoundsPlayed > 0 ? (double)s.Damage / s.RoundsPlayed : 0)),
                    AverageKDRatio = g.Average(x => x.Stats.Average(s => s.Deaths > 0 ? (double)s.Kills / s.Deaths : s.Kills))
                })
                .ToList();

            foreach (var stat in mapStats)
            {
                stat.WinRate = stat.MatchesPlayed > 0 ? (double)stat.Wins / stat.MatchesPlayed : 0;
            }

            return mapStats;
        }

        private async Task<List<ChampionStatsModel>> GetChampionStats(List<PlayerStatistic_LoL> lolStats)
        {
            return lolStats
                .GroupBy(s => s.ChampionId)
                .Select(g => new ChampionStatsModel
                {
                    ChampionId = g.Key,
                    ChampionName = g.First().ChampionName,
                    MatchesPlayed = g.Count(),
                    Wins = g.Count(s => s.IsWinner),
                    AverageKDRatio = (double)g.Average(s => s.Deaths > 0 ? (double)(s.Kills ?? 0) / s.Deaths : (s.Kills ?? 0)),
                    AverageKPARatio = (double)g.Average(s => s.Deaths > 0 ? (double)((s.Kills ?? 0) + (s.Assists ?? 0)) / s.Deaths : ((s.Kills ?? 0) + (s.Assists ?? 0)))
                })
                .ToList();
        }

        private async Task<List<LaneStatsModel>> GetLaneStats(List<PlayerStatistic_LoL> lolStats)
        {
            return lolStats
                .GroupBy(s => s.Lane)
                .Select(g => new LaneStatsModel
                {
                    Lane = g.Key,
                    MatchesPlayed = g.Count(),
                    Wins = g.Count(s => s.IsWinner),
                    AverageKDRatio = (double)g.Average(s => s.Deaths > 0 ? (double)(s.Kills ?? 0) / s.Deaths : (s.Kills ?? 0))
                })
                .ToList();
        }

        private async Task<List<string>> GetTeamsPlayedFor(int playerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.PlayerTeamTransfers
                .Include(t => t.Team)
                .Where(t => t.PlayerId == playerId);

            if (startDate.HasValue)
                query = query.Where(t => t.StartDate <= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.EndDate == null || t.EndDate >= endDate.Value);

            var transfers = await query.ToListAsync();
            return transfers.Select(t => t.Team.FullName).Distinct().ToList();
        }

        private async Task<int?> GetPlayerIdFromUuid(string uuid, string provider)
        {
            var gameProfile = await _context.GameProfiles
                .FirstOrDefaultAsync(gp => gp.UUID == uuid && gp.Provider == provider);
            return gameProfile?.PlayerId;
        }
    }
}
