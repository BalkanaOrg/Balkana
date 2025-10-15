using Balkana.Data.DTOs.Riot;
using Balkana.Data.Models;
using Balkana.Data;
using Balkana.Data.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Matches.Models
{
    public class RiotMatchImporter : IMatchImporter
    {
        private readonly HttpClient _http;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public RiotMatchImporter(HttpClient http, ApplicationDbContext db, IConfiguration config)
        {
            _http = http;
            _db = db;
            _config = config;
        }

        public async Task<List<Match>> ImportMatchAsync(string matchId, ApplicationDbContext db)
        {
            // fetch from Riot API - matchId format: EUW1_1234567890 or EUNE1_1234567890
            var region = "europe"; // routing region for match data
            var response = await _http.GetFromJsonAsync<RiotMatchDto>(
                $"lol/match/v5/matches/{matchId}");

            if (response == null) return null;

            // Determine match date from timestamp
            var matchDate = DateTimeOffset.FromUnixTimeMilliseconds(response.info.gameStartTimestamp).UtcDateTime;

            // Map players by PUUID
            var bluePuuids = response.info.participants.Where(p => p.teamId == 100).Select(p => p.puuid).ToList();
            var redPuuids = response.info.participants.Where(p => p.teamId == 200).Select(p => p.puuid).ToList();

            // Try to resolve teams based on player PUUIDs in GameProfiles
            var dbBlueTeam = await ResolveTeamAsync(db, bluePuuids, matchDate);
            var dbRedTeam = await ResolveTeamAsync(db, redPuuids, matchDate);

            // Determine winner team
            var blueWon = response.info.teams.FirstOrDefault(t => t.teamId == 100)?.win ?? false;
            Team winnerTeam = blueWon ? dbBlueTeam : dbRedTeam;

            var match = new MatchLoL
            {
                ExternalMatchId = response.metadata.matchId,
                Source = "RIOT",
                PlayedAt = matchDate,
                IsCompleted = true,
                TeamA = dbBlueTeam,
                TeamASourceSlot = "Blue",
                TeamB = dbRedTeam,
                TeamBSourceSlot = "Red",
                WinnerTeam = winnerTeam,
                GameVersion = response.info.gameVersion,
                MapId = response.info.mapId,
                GameMode = response.info.gameMode,
                PlayerStats = new List<PlayerStatistic>()
            };

            // Add player stats
            foreach (var participant in response.info.participants)
            {
                var stat = new PlayerStatistic_LoL
                {
                    Match = match,
                    PlayerUUID = participant.puuid,
                    Source = "RIOT",

                    Kills = participant.kills,
                    Deaths = participant.deaths,
                    Assists = participant.assists,

                    ChampionId = participant.championId,
                    ChampionName = participant.championName,
                    Lane = participant.teamPosition,

                    GoldEarned = participant.goldEarned,
                    CreepScore = participant.totalMinionsKilled + participant.neutralMinionsKilled,
                    VisionScore = participant.visionScore,

                    TotalDamageToChampions = participant.totalDamageDealtToChampions,
                    TotalDamageToObjectives = participant.damageDealtToObjectives,

                    Item0 = participant.item0,
                    Item1 = participant.item1,
                    Item2 = participant.item2,
                    Item3 = participant.item3,
                    Item4 = participant.item4,
                    Item5 = participant.item5,
                    Item6 = participant.item6
                };

                match.PlayerStats.Add(stat);
            }

            return new List<Match> { match };
        }

        public async Task<ICollection<ExternalMatchSummary>> GetMatchHistoryAsync(string profileId)
        {
            // Riot PUUID match history endpoint
            var matchIds = await _http.GetFromJsonAsync<List<string>>(
                $"lol/match/v5/matches/by-puuid/{profileId}/ids?count=20");

            var results = new List<ExternalMatchSummary>();

            if (matchIds == null || !matchIds.Any())
                return results;

            foreach (var id in matchIds)
            {
                // fetch metadata for each match
                var match = await _http.GetFromJsonAsync<RiotMatchDto>(
                    $"lol/match/v5/matches/{id}");

                if (match != null)
                {
                    results.Add(new ExternalMatchSummary
                    {
                        ExternalMatchId = id,
                        Source = "RIOT",
                        PlayedAt = DateTimeOffset.FromUnixTimeMilliseconds(match.info.gameStartTimestamp).UtcDateTime
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Resolve a team based on player PUUIDs at a specific date
        /// </summary>
        private async Task<Team> ResolveTeamAsync(ApplicationDbContext db, List<string> puuids, DateTime matchDate)
        {
            if (puuids == null || !puuids.Any())
                return null;

            // Find teams that had at least 3 players from the puuid list at the match date
            var teams = await db.Teams
                .Include(t => t.Transfers)
                    .ThenInclude(tr => tr.Player)
                        .ThenInclude(p => p.GameProfiles)
                .Where(t => t.Transfers.Any(tr =>
                    (tr.StartDate == null || tr.StartDate <= matchDate) &&
                    (tr.EndDate == null || tr.EndDate > matchDate) &&
                    tr.Player.GameProfiles.Any(gp => gp.Provider == "RIOT" && puuids.Contains(gp.UUID))
                ))
                .ToListAsync();

            // Find team with most matching players
            Team bestMatch = null;
            int maxMatches = 0;

            foreach (var team in teams)
            {
                var activePlayerPuuids = team.Transfers
                    .Where(tr =>
                        (tr.StartDate == null || tr.StartDate <= matchDate) &&
                        (tr.EndDate == null || tr.EndDate > matchDate))
                    .SelectMany(tr => tr.Player.GameProfiles.Where(gp => gp.Provider == "RIOT").Select(gp => gp.UUID))
                    .ToList();

                var matchCount = puuids.Count(p => activePlayerPuuids.Contains(p));

                if (matchCount > maxMatches)
                {
                    maxMatches = matchCount;
                    bestMatch = team;
                }
            }

            // Only return team if at least 3 players matched
            return maxMatches >= 3 ? bestMatch : null;
        }
    }
}
