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

        public RiotMatchImporter(HttpClient http, ApplicationDbContext db)
        {
            _http = http;
            _db = db;
        }

        public async Task<List<Match>> ImportMatchAsync(string matchId, ApplicationDbContext db)
        {
            // fetch from Riot API
            var response = await _http.GetFromJsonAsync<RiotMatchDto>(
                $"https://europe.api.riotgames.com/lol/match/v5/matches/{matchId}?api_key=YOUR_KEY");

            if (response == null) return null;

            // Map players
            var bluePuuids = response.info.participants.Where(p => p.teamId == 100).Select(p => p.puuid).ToList();
            var redPuuids = response.info.participants.Where(p => p.teamId == 200).Select(p => p.puuid).ToList();

            var dbBlueTeam = db.Teams.Include(t => t.Transfers).ThenInclude(tr => tr.Player)
                .FirstOrDefault(t => t.Transfers.Any(tr => tr.Player.GameProfiles.Any(gp => bluePuuids.Contains(gp.UUID))));

            var dbRedTeam = db.Teams.Include(t => t.Transfers).ThenInclude(tr => tr.Player)
                .FirstOrDefault(t => t.Transfers.Any(tr => tr.Player.GameProfiles.Any(gp => redPuuids.Contains(gp.UUID))));

            var match = new MatchLoL
            {
                ExternalMatchId = response.metadata.matchId,
                Source = "RIOT",
                PlayedAt = DateTimeOffset.FromUnixTimeMilliseconds(response.info.gameStartTimestamp).UtcDateTime,
                IsCompleted = true,
                TeamA = dbBlueTeam,
                TeamASourceSlot = "Blue",
                TeamB = dbRedTeam,
                TeamBSourceSlot = "Red",
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
                $"https://europe.api.riotgames.com/lol/match/v5/matches/by-puuid/{profileId}/ids?count=20&api_key=YOUR_KEY");

            var results = new List<ExternalMatchSummary>();

            foreach (var id in matchIds)
            {
                // fetch metadata for each match
                var match = await _http.GetFromJsonAsync<RiotMatchDto>(
                    $"https://europe.api.riotgames.com/lol/match/v5/matches/{id}?api_key=YOUR_KEY");

                results.Add(new ExternalMatchSummary
                {
                    ExternalMatchId = id,
                    Source = "RIOT",
                    PlayedAt = DateTimeOffset.FromUnixTimeMilliseconds(match.info.gameStartTimestamp).UtcDateTime
                });
            }

            return results;
        }
    }
}
