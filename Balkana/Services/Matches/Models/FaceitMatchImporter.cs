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

        public FaceitMatchImporter(HttpClient http, ApplicationDbContext db)
        {
            _http = http;
            _db = db;
        }
        private string _hubId;

        public void SetHubId(string hubId)
        {
            _hubId = hubId;
        }

        public async Task<List<Match>> ImportMatchAsync(string matchId, ApplicationDbContext db)
        {
            // 1. Get base match info
            var response = await _http.GetFromJsonAsync<FaceItMatchDto>($"matches/{matchId}");
            if (response == null) return null;

            // 2. Get detailed stats
            var statsResponse = await _http.GetFromJsonAsync<FaceItMatchStatsDto>($"matches/{matchId}/stats");
            if (statsResponse == null || statsResponse.rounds == null) return null;

            var matches = new List<Match>();

            // 3. Resolve teams once (same across maps)
            var firstRound = statsResponse.rounds.First();
            var firstTeamStats = firstRound.teams;

            var dbTeam1 = await ResolveTeamAsync(db, firstTeamStats[0].players.Select(p => p.player_id));
            var dbTeam2 = await ResolveTeamAsync(db, firstTeamStats[1].players.Select(p => p.player_id));

            // 4. Loop over each map
            foreach (var round in statsResponse.rounds)
            {
                var teamStats = round.teams;
                var totalRounds = int.Parse(round.round_stats.Rounds ?? "0");

                var match = new MatchCS
                {
                    ExternalMatchId = $"{response.match_id}-{round.round_stats.Map}",
                    Source = "FACEIT",
                    PlayedAt = DateTimeOffset.FromUnixTimeSeconds(response.started_at).UtcDateTime,
                    IsCompleted = response.status == "FINISHED",
                    CompetitionType = response.competition_name,

                    MapId = await ResolveMapAsync(db, round.round_stats.Map),

                    TeamA = dbTeam1,
                    TeamASourceSlot = "Team1",
                    TeamB = dbTeam2,
                    TeamBSourceSlot = "Team2",
                    PlayerStats = new List<PlayerStatistic>()
                };

                // Add per-player stats for this map
                foreach (var team in teamStats)
                {
                    foreach (var player in team.players)
                    {
                        int kills = int.Parse(player.player_stats.GetValueOrDefault("Kills", "0"));
                        int deaths = int.Parse(player.player_stats.GetValueOrDefault("Deaths", "0"));
                        int assists = int.Parse(player.player_stats.GetValueOrDefault("Assists", "0"));

                        int _2k = int.Parse(player.player_stats.GetValueOrDefault("Double Kills", "0"));
                        int _3k = int.Parse(player.player_stats.GetValueOrDefault("Triple Kills", "0"));
                        int _4k = int.Parse(player.player_stats.GetValueOrDefault("Quadro Kills", "0"));
                        int _5k = int.Parse(player.player_stats.GetValueOrDefault("Penta Kills", "0"));

                        int oneK = Math.Max(0, kills - (_2k + _3k + _4k + _5k));

                        int roundsPlayed = totalRounds > 0 ? totalRounds : 1;
                        double kd = deaths > 0 ? (double)kills / deaths : kills;  // avoid div by 0
                        double kr = (double)kills / roundsPlayed;
                        double rating = (0.0073 * kills) + (0.3591 * kd) + (0.5329 * kr);

                        match.PlayerStats.Add(new PlayerStatistic_CS2
                        {
                            Match = match,
                            MatchId = match.Id, // will be set once saved
                            PlayerUUID = player.player_id,
                            Source = "FACEIT",
                            Team = team.team_id,
                            RoundsPlayed = totalRounds,

                            Kills = int.Parse(player.player_stats.GetValueOrDefault("Kills", "0")),
                            Deaths = int.Parse(player.player_stats.GetValueOrDefault("Deaths", "0")),
                            Assists = int.Parse(player.player_stats.GetValueOrDefault("Assists", "0")),
                            HSkills = int.Parse(player.player_stats.GetValueOrDefault("Headshots", "0")),
                            Damage = int.Parse(player.player_stats.GetValueOrDefault("Damage", "0")),
                            FK = int.Parse(player.player_stats.GetValueOrDefault("First Kills", "0")),
                            FD = int.Parse(player.player_stats.GetValueOrDefault("First Deaths", "0")),
                            HLTV1 = rating,
                            _1k = oneK,
                            _2k = _2k,
                            _3k = _3k,
                            _4k = _4k,
                            _5k = _5k,
                            SniperKills = int.Parse(player.player_stats.GetValueOrDefault("Sniper Kills", "0")),
                            PistolKills = int.Parse(player.player_stats.GetValueOrDefault("Pistol Kills", "0")),
                            KnifeKills = int.Parse(player.player_stats.GetValueOrDefault("Knife Kills", "0")),
                            UtilityUsage = int.Parse(player.player_stats.GetValueOrDefault("Utility Count", "0")),
                            Flashes = int.Parse(player.player_stats.GetValueOrDefault("Flash Successes", "0")),
                        });
                    }
                }

                matches.Add(match);
            }

            return matches;
        }

        public async Task<ICollection<ExternalMatchSummary>> GetMatchHistoryAsync(string _)
        {
            // ✅ Always pull from HUB
            var response = await _http.GetFromJsonAsync<FaceitHistoryDTO>(
                $"hubs/{_hubId}/matches?offset=0&limit=20");

            if (response?.items == null) return new List<ExternalMatchSummary>();

            var matchIds = response.items.Select(m => m.match_id).ToList();

            var existing = await _db.Matches
                .Where(m => m.Source == "FACEIT" && matchIds.Any(id => m.ExternalMatchId.StartsWith(id)))
                .Select(m => m.ExternalMatchId)
                .ToListAsync();

            return response.items.Select(m => new ExternalMatchSummary
            {
                ExternalMatchId = m.match_id,
                Source = "FACEIT",
                PlayedAt = DateTimeOffset.FromUnixTimeSeconds(m.started_at).UtcDateTime,
                ExistsInDb = existing.Any(e => e.StartsWith(m.match_id))
            }).ToList();
        }

        private async Task<int> ResolveMapAsync(ApplicationDbContext db, string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
                return 0;

            var normalized = mapName.ToLower().Trim();

            var map = await db.GameMaps
                .FirstOrDefaultAsync(m => m.Name.ToLower() == normalized);

            if (map == null)
                throw new InvalidOperationException($"Map '{mapName}' not found in database.");

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
