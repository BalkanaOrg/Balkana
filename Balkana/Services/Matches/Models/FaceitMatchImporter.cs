using Balkana.Data.DTOs.FaceIt;
using Balkana.Data.Models;
using Balkana.Data;
using Balkana.Data.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Matches.Models
{
    public class FaceitMatchImporter : IMatchImporter
    {
        private readonly HttpClient _http;
        private readonly ApplicationDbContext _db;
        private readonly string _hubId;

        public FaceitMatchImporter(HttpClient http, ApplicationDbContext db, IConfiguration config)
        {
            _http = http;
            _db = db;
            _hubId = config["Faceit:HubId"];
        }

        public async Task<Match> ImportMatchAsync(string matchId, ApplicationDbContext db)
        {
            // 1. Get base match info
            var response = await _http.GetFromJsonAsync<FaceItMatchDto>($"matches/{matchId}");
            if (response == null) return null;

            // 2. Get detailed stats
            var statsResponse = await _http.GetFromJsonAsync<FaceItMatchStatsDto>($"matches/{matchId}/stats");
            if (statsResponse == null || statsResponse.rounds == null) return null;

            var round = statsResponse.rounds.First(); // usually only one round in FACEIT hub matches
            var teamStats = round.teams;

            // 3. Resolve teams from DB
            var dbTeam1 = await ResolveTeamAsync(db, teamStats[0].players.Select(p => p.player_id));
            var dbTeam2 = await ResolveTeamAsync(db, teamStats[1].players.Select(p => p.player_id));

            var match = new MatchCS
            {
                ExternalMatchId = response.match_id,
                Source = "FACEIT",
                PlayedAt = DateTimeOffset.FromUnixTimeSeconds(response.started_at).UtcDateTime,
                IsCompleted = response.status == "FINISHED",
                CompetitionType = response.competition_name,
                MapId = await ResolveMapAsync(db, response),
                TeamA = dbTeam1,
                TeamASourceSlot = "Team1",
                TeamB = dbTeam2,
                TeamBSourceSlot = "Team2",
                PlayerStats = new List<PlayerStatistic>(new List<PlayerStatistic_CS2>()) // Explicit conversion
            };

            var totalRounds = int.Parse(round.round_stats?.Rounds ?? "0");
            // 4. Attach player stats
            foreach (var team in teamStats)
            {
                foreach (var player in team.players)
                {
                    match.PlayerStats.Add(new PlayerStatistic_CS2
                    {
                        Match = match,
                        MatchId = match.Id, // will be set once saved
                        PlayerUUID = player.player_id,  // ✅ store raw UUID
                        Source = "FACEIT",
                        Team = team.team_id,           // FACEIT provides team_id (can be "faction1"/"faction2")
                        //IsWinner = team.team_stats.GetValueOrDefault("Team Win", "0") == "1",

                        Kills = int.Parse(player.player_stats.GetValueOrDefault("Kills", "0")),
                        Deaths = int.Parse(player.player_stats.GetValueOrDefault("Deaths", "0")),
                        Assists = int.Parse(player.player_stats.GetValueOrDefault("Assists", "0")),
                        HSkills = int.Parse(player.player_stats.GetValueOrDefault("Headshots", "0")),
                        Damage = int.Parse(player.player_stats.GetValueOrDefault("Damage", "0")),
                        RoundsPlayed = totalRounds,
                        FK = int.Parse(player.player_stats.GetValueOrDefault("First Kills", "0")),
                        FD = int.Parse(player.player_stats.GetValueOrDefault("First Deaths", "0")),
                        _2k = int.Parse(player.player_stats.GetValueOrDefault("Double Kills", "0")),
                        _3k = int.Parse(player.player_stats.GetValueOrDefault("Triple Kills", "0")),
                        _4k = int.Parse(player.player_stats.GetValueOrDefault("Quatro Kills", "0")),
                        _5k = int.Parse(player.player_stats.GetValueOrDefault("Penta Kills", "0")),
                        SniperKills = int.Parse(player.player_stats.GetValueOrDefault("Sniper Kills", "0")),
                        PistolKills = int.Parse(player.player_stats.GetValueOrDefault("Pistol Kills", "0")),
                        KnifeKills = int.Parse(player.player_stats.GetValueOrDefault("Knife Kills", "0")),
                        UtilityUsage = int.Parse(player.player_stats.GetValueOrDefault("Utility Count", "0")),
                        Flashes = int.Parse(player.player_stats.GetValueOrDefault("Flash Successes", "0")),
                        // etc. add the rest if Faceit API returns them
                    });
                }
            }

            return match;
        }

        public async Task<ICollection<ExternalMatchSummary>> GetMatchHistoryAsync(string _)
        {
            // ✅ Always pull from HUB
            var response = await _http.GetFromJsonAsync<FaceitHistoryDTO>(
                $"hubs/{_hubId}/matches?offset=0&limit=20");

            if (response?.items == null) return new List<ExternalMatchSummary>();

            return response.items.Select(m => new ExternalMatchSummary
            {
                ExternalMatchId = m.match_id,
                Source = "FACEIT",
                PlayedAt = DateTimeOffset.FromUnixTimeSeconds(m.started_at).UtcDateTime
            }).ToList();
        }

        private async Task<int> ResolveMapAsync(ApplicationDbContext db, FaceItMatchDto response)
        {
            var mapId = response.voting?.map?.pick?.FirstOrDefault();
            if (string.IsNullOrEmpty(mapId))
                throw new Exception($"Match {response.match_id} has no map defined.");

            var map = await db.GameMaps.FirstOrDefaultAsync(m => m.Name == mapId);
            if (map == null)
                throw new Exception($"Map '{mapId}' for match {response.match_id} does not exist in DB.");

            return map.Id;
        }

        private async Task<Team> ResolveTeamAsync(ApplicationDbContext db, IEnumerable<string> playerIds)
        {
            return await db.Teams
                .Include(t => t.Transfers).ThenInclude(tr => tr.Player).ThenInclude(pl => pl.GameProfiles)
                .FirstOrDefaultAsync(t => t.Transfers.Any(tr => tr.Player.GameProfiles.Any(gp => playerIds.Contains(gp.UUID))));
        }
    }
}
