using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Tournaments;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Tournaments
{
    public class TournamentPlacementPointsService
    {
        private readonly ApplicationDbContext _context;

        public TournamentPlacementPointsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TeamPointsPreviewRow>> BuildPointsPreviewAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
            if (tournament == null)
                return new List<TeamPointsPreviewRow>();

            var teams = await _context.TournamentTeams
                .Include(tt => tt.Team)
                .Where(tt => tt.TournamentId == tournamentId)
                .ToListAsync();

            var bracketService = new TournamentBracketPlacementService(_context);
            var placements = await bracketService.BuildPlacementsListAsync(tournamentId);
            var byTeam = placements.ToDictionary(p => p.TeamId);

            var participatingCount = teams.Count;
            var bracketComplete = participatingCount > 0 && placements.Count == participatingCount;

            var rows = new List<TeamPointsPreviewRow>();
            foreach (var tt in teams.OrderBy(tt => tt.Team?.FullName))
            {
                var teamId = tt.TeamId;
                var esCount = await CountEmergencySubstitutesAsync(teamId, tournament);
                var penaltyPct = Math.Min(100, 20 * esCount);

                var row = new TeamPointsPreviewRow
                {
                    TeamId = teamId,
                    TeamName = tt.Team?.FullName ?? $"Team {teamId}",
                    EmergencySubstituteCount = esCount,
                    PenaltyPercent = penaltyPct,
                    BracketPreviewIncomplete = !bracketComplete
                };

                if (byTeam.TryGetValue(teamId, out var pl))
                {
                    var p = TournamentPlacementScoring.GetPointsForPlacement(tournament, pl.Placement);
                    var factor = 1 - Math.Min(1.0, 0.2 * esCount);
                    var pEff = (int)Math.Round(p * factor);
                    row.Placement = pl.Placement;
                    row.BasePointsFromPlacement = p;
                    row.EffectivePlayerPool = pEff;
                    row.OrganisationPoints = (int)Math.Round(0.2 * pEff);
                    row.PrizePoolAwarded = pl.PrizePoolAwarded;
                }

                rows.Add(row);
            }

            return rows;
        }

        public async Task DistributeTournamentPlacementPointsAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Placements)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
            if (tournament == null)
                return;

            var existing = await _context.PlayerPoints
                .Where(pp => pp.TournamentId == tournamentId)
                .ToListAsync();
            _context.PlayerPoints.RemoveRange(existing);

            foreach (var placement in tournament.Placements)
            {
                var p = TournamentPlacementScoring.GetPointsForPlacement(tournament, placement.Placement);
                var esCount = await CountEmergencySubstitutesAsync(placement.TeamId, tournament);
                var factor = 1 - Math.Min(1.0, 0.2 * esCount);
                var pEff = (int)Math.Round(p * factor);
                var org = (int)Math.Round(0.2 * pEff);

                placement.PointsAwarded = pEff;
                placement.OrganisationPointsAwarded = org;

                var counts = await GetPlayerMapCountsForTeamAsync(
                    placement.TeamId,
                    tournamentId,
                    tournament.StartDate,
                    tournament.EndDate);

                var eligible = counts
                    .Where(kv => kv.Value > 0)
                    .Select(kv => (PlayerId: kv.Key, Games: kv.Value))
                    .ToList();

                if (!eligible.Any())
                    continue;

                var splits = SplitPlayerPool(pEff, eligible);
                foreach (var (playerId, pts) in splits)
                {
                    if (pts <= 0)
                        continue;
                    _context.PlayerPoints.Add(new PlayerPoints
                    {
                        PlayerId = playerId,
                        TournamentId = tournamentId,
                        PointsAwarded = pts
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private Task<int> CountEmergencySubstitutesAsync(int teamId, Tournament tournament)
        {
            return _context.PlayerTeamTransfers
                .Where(t => t.TeamId == teamId
                            && t.Status == PlayerTeamStatus.EmergencySubstitute
                            && t.StartDate <= tournament.EndDate
                            && (t.EndDate == null || t.EndDate >= tournament.StartDate))
                .CountAsync();
        }

        private async Task<Dictionary<int, int>> GetPlayerMapCountsForTeamAsync(
            int teamId,
            int tournamentId,
            DateTime tournamentStart,
            DateTime tournamentEnd)
        {
            var matchIds = await _context.Series
                .Where(s => s.TournamentId == tournamentId)
                .SelectMany(s => s.Matches)
                .Where(m => m.IsCompleted
                            && m.PlayedAt >= tournamentStart
                            && m.PlayedAt <= tournamentEnd)
                .Select(m => m.Id)
                .Distinct()
                .ToListAsync();

            if (!matchIds.Any())
                return new Dictionary<int, int>();

            var stats = await _context.PlayerStatsCS
                .Where(ps => matchIds.Contains(ps.MatchId) && ps.Source == "FACEIT")
                .Include(ps => ps.Match)
                .ToListAsync();

            var uuids = stats.Select(s => s.PlayerUUID).Distinct().ToList();
            var profileRows = await _context.GameProfiles
                .Where(gp => uuids.Contains(gp.UUID) && gp.Provider == "FACEIT")
                .Select(gp => new { gp.UUID, gp.PlayerId })
                .ToListAsync();
            var uuidToPlayerId = profileRows
                .GroupBy(x => x.UUID)
                .ToDictionary(g => g.Key, g => g.First().PlayerId);

            var playerIds = uuidToPlayerId.Values.Distinct().ToList();
            var transfers = await _context.PlayerTeamTransfers
                .Where(t => playerIds.Contains(t.PlayerId) && t.StartDate <= tournamentEnd)
                .ToListAsync();

            var pairs = new HashSet<(int PlayerId, int MatchId)>();

            foreach (var ps in stats)
            {
                if (!uuidToPlayerId.TryGetValue(ps.PlayerUUID, out var pid))
                    continue;
                var match = ps.Match;
                if (match == null)
                    continue;

                var tr = transfers
                    .Where(t => t.PlayerId == pid
                                && t.StartDate <= match.PlayedAt
                                && (t.EndDate == null || t.EndDate >= match.PlayedAt))
                    .OrderByDescending(t => t.StartDate)
                    .FirstOrDefault();

                if (tr?.TeamId == teamId)
                    pairs.Add((pid, match.Id));
            }

            return pairs
                .GroupBy(x => x.PlayerId)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Split integer pool across players by map counts (largest remainder), or equally if exactly 5 players with identical counts.
        /// </summary>
        private static Dictionary<int, int> SplitPlayerPool(int pool, List<(int PlayerId, int Games)> playersWithGames)
        {
            var result = new Dictionary<int, int>();
            if (pool <= 0 || playersWithGames.Count == 0)
                return result;

            var ordered = playersWithGames.OrderBy(x => x.PlayerId).ToList();
            var sum = ordered.Sum(x => x.Games);
            if (sum == 0)
                return result;

            if (ordered.Count == 5 && ordered.All(x => x.Games == ordered[0].Games))
                return EqualSplitAmongPlayers(pool, ordered.Select(x => x.PlayerId).ToList());

            var weighted = ordered.Select(x =>
            {
                var raw = pool * (double)x.Games / sum;
                var fl = (int)Math.Floor(raw);
                return (x.PlayerId, Floor: fl, Frac: raw - fl);
            }).ToList();

            var deficit = pool - weighted.Sum(x => x.Floor);
            var order = weighted
                .OrderByDescending(x => x.Frac)
                .ThenBy(x => x.PlayerId)
                .ToList();

            foreach (var x in weighted)
                result[x.PlayerId] = x.Floor;

            for (var j = 0; j < deficit && j < order.Count; j++)
                result[order[j].PlayerId] += 1;

            return result;
        }

        private static Dictionary<int, int> EqualSplitAmongPlayers(int pool, List<int> playerIdsAscending)
        {
            var dict = new Dictionary<int, int>();
            var n = playerIdsAscending.Count;
            if (n == 0)
                return dict;

            var b = pool / n;
            var r = pool % n;
            for (var i = 0; i < n; i++)
                dict[playerIdsAscending[i]] = b + (i < r ? 1 : 0);

            return dict;
        }
    }
}
