using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Tournaments;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Tournaments
{
    public class MVPEVPService
    {
        private readonly ApplicationDbContext _context;

        public MVPEVPService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PlayerMVPCandidate>> GetMVPCandidatesAsync(int tournamentId, MVFormulaConfiguration formulaConfig)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return new List<PlayerMVPCandidate>();

            // Determine which matches to include based on tournament size
            var eligibleSeries = GetEligibleSeriesForMVP(tournament);
            var seriesIds = eligibleSeries.Select(s => s.Id).ToList();

            // Get all players who played in eligible matches
            var playerStats = await _context.PlayerStatsCS
                .Include(ps => ps.Match)
                    .ThenInclude(m => m.Series)
                .Where(ps => seriesIds.Contains(ps.Match.SeriesId))
                .ToListAsync();

            // Group by player UUID and calculate aggregate stats
            var playerGroups = playerStats.GroupBy(ps => ps.PlayerUUID);

            var candidates = new List<PlayerMVPCandidate>();

            foreach (var group in playerGroups)
            {
                var stats = group.ToList();
                var playerUUID = group.Key;

                // Get player from GameProfile using UUID
                var player = await GetPlayerByUUIDAsync(playerUUID);
                if (player == null) continue;

                // Get player's team from the tournament
                var team = await GetPlayerTeamInTournamentAsync(tournamentId, player.Id);
                if (team == null) continue;

                var formulaScore = CalculateFormulaScore(stats, formulaConfig);

                candidates.Add(new PlayerMVPCandidate
                {
                    PlayerId = player.Id,
                    PlayerName = player.Nickname,
                    TeamName = team.FullName,
                    TeamId = team.Id,
                    IsInEligibleMatches = true,
                    FormulaScore = formulaScore
                });
            }

            return candidates.OrderByDescending(c => c.FormulaScore.TotalScore).ToList();
        }

        public async Task<List<PlayerEVPCandidate>> GetEVPCandidatesAsync(int tournamentId, MVFormulaConfiguration formulaConfig)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return new List<PlayerEVPCandidate>();

            // Determine which matches to include based on tournament size
            var eligibleSeries = GetEligibleSeriesForEVP(tournament);
            var seriesIds = eligibleSeries.Select(s => s.Id).ToList();

            // Get all players who played in eligible matches
            var playerStats = await _context.PlayerStatsCS
                .Include(ps => ps.Match)
                    .ThenInclude(m => m.Series)
                .Where(ps => seriesIds.Contains(ps.Match.SeriesId))
                .ToListAsync();

            // Group by player UUID and calculate aggregate stats
            var playerGroups = playerStats.GroupBy(ps => ps.PlayerUUID);

            var candidates = new List<PlayerEVPCandidate>();

            foreach (var group in playerGroups)
            {
                var stats = group.ToList();
                var playerUUID = group.Key;

                // Get player from GameProfile using UUID
                var player = await GetPlayerByUUIDAsync(playerUUID);
                if (player == null) continue;

                // Get player's team from the tournament
                var team = await GetPlayerTeamInTournamentAsync(tournamentId, player.Id);
                if (team == null) continue;

                var formulaScore = CalculateFormulaScore(stats, formulaConfig);

                candidates.Add(new PlayerEVPCandidate
                {
                    PlayerId = player.Id,
                    PlayerName = player.Nickname,
                    TeamName = team.FullName,
                    TeamId = team.Id,
                    IsInEligibleMatches = true,
                    FormulaScore = formulaScore
                });
            }

            return candidates.OrderByDescending(c => c.FormulaScore.TotalScore).ToList();
        }

        private List<Balkana.Data.Models.Series> GetEligibleSeriesForMVP(Tournament tournament)
        {
            var allSeries = tournament.Series.ToList();
            var teamCount = tournament.TournamentTeams.Count;

            return teamCount switch
            {
                >= 4 and <= 8 => GetFinalAndSemiFinalSeries(allSeries), // 4-8 teams: Final and Semi-finals only
                >= 9 and <= 16 => GetQuarterFinalAndBeyond(allSeries), // 9-16 teams: Quarter-finals and beyond
                _ => GetFinalAndSemiFinalSeries(allSeries) // Default to final and semi-finals
            };
        }

        private List<Balkana.Data.Models.Series> GetEligibleSeriesForEVP(Tournament tournament)
        {
            var allSeries = tournament.Series.ToList();
            var teamCount = tournament.TournamentTeams.Count;

            return teamCount switch
            {
                4 => GetFinalSeries(allSeries), // 4 teams: Final only
                >= 5 and <= 8 => GetFinalAndSemiFinalSeries(allSeries), // 5-8 teams: Final and Semi-finals
                >= 9 and <= 16 => GetQuarterFinalAndBeyond(allSeries), // 9-16 teams: Quarter-finals and beyond
                _ => GetFinalAndSemiFinalSeries(allSeries) // Default to final and semi-finals
            };
        }

        private List<Balkana.Data.Models.Series> GetFinalSeries(List<Balkana.Data.Models.Series> allSeries)
        {
            // For double elimination, prioritize Grand Final
            if (allSeries.Any(s => s.Bracket == BracketType.GrandFinal))
            {
                return allSeries.Where(s => s.Bracket == BracketType.GrandFinal).ToList();
            }
            
            // For single elimination, find the final round (highest round number)
            var maxRound = allSeries.Max(s => s.Round);
            return allSeries.Where(s => s.Round == maxRound).ToList();
        }

        private List<Balkana.Data.Models.Series> GetFinalAndSemiFinalSeries(List<Balkana.Data.Models.Series> allSeries)
        {
            var finalSeries = GetFinalSeries(allSeries);
            var result = new List<Balkana.Data.Models.Series>(finalSeries);

            // For double elimination
            if (allSeries.Any(s => s.Bracket == BracketType.GrandFinal))
            {
                // Add Upper Bracket Final and Lower Bracket Final
                var upperBracketFinal = allSeries
                    .Where(s => s.Bracket == BracketType.Upper)
                    .OrderByDescending(s => s.Round)
                    .FirstOrDefault();
                if (upperBracketFinal != null) result.Add(upperBracketFinal);

                var lowerBracketFinal = allSeries
                    .Where(s => s.Bracket == BracketType.Lower)
                    .OrderByDescending(s => s.Round)
                    .FirstOrDefault();
                if (lowerBracketFinal != null) result.Add(lowerBracketFinal);
            }
            else
            {
                // For single elimination, add semi-finals
                var finalRound = finalSeries.First().Round;
                var semiFinalRound = finalRound - 1;
                var semiFinalSeries = allSeries.Where(s => s.Round == semiFinalRound).ToList();
                result.AddRange(semiFinalSeries);
            }
            
            return result.Distinct().ToList();
        }

        private List<Balkana.Data.Models.Series> GetQuarterFinalAndBeyond(List<Balkana.Data.Models.Series> allSeries)
        {
            var finalAndSemiSeries = GetFinalAndSemiFinalSeries(allSeries);
            var result = new List<Balkana.Data.Models.Series>(finalAndSemiSeries);

            // For double elimination
            if (allSeries.Any(s => s.Bracket == BracketType.GrandFinal))
            {
                // Add Upper Bracket Semi-finals and Lower Bracket Semi-finals
                var upperBracketSemi = allSeries
                    .Where(s => s.Bracket == BracketType.Upper)
                    .OrderByDescending(s => s.Round)
                    .Skip(1)
                    .Take(1)
                    .ToList();
                result.AddRange(upperBracketSemi);

                var lowerBracketSemi = allSeries
                    .Where(s => s.Bracket == BracketType.Lower)
                    .OrderByDescending(s => s.Round)
                    .Skip(1)
                    .Take(1)
                    .ToList();
                result.AddRange(lowerBracketSemi);
            }
            else
            {
                // For single elimination, add quarter-finals
                var finalRound = finalAndSemiSeries.Max(s => s.Round);
                var quarterFinalRound = finalRound - 2;
                var quarterFinalSeries = allSeries.Where(s => s.Round == quarterFinalRound).ToList();
                result.AddRange(quarterFinalSeries);
            }
            
            return result.Distinct().ToList();
        }

        private MVFormulaScore CalculateFormulaScore(List<PlayerStatistic_CS2> stats, MVFormulaConfiguration config)
        {
            if (!stats.Any()) return new MVFormulaScore();

            var totalRounds = stats.Sum(s => s.RoundsPlayed);
            var totalKills = stats.Sum(s => s.Kills);
            var totalDeaths = stats.Sum(s => s.Deaths);
            var totalDamage = stats.Sum(s => s.Damage);
            var totalAWPkills = stats.Sum(s => s.SniperKills);
            var totalFirstKills = stats.Sum(s => s.FK);
            var totalUtilityDamage = stats.Sum(s => s.UtilityUsage);

            var avgHLTV = stats.Average(s => s.HLTV1);
            var killPerRound = totalRounds > 0 ? (double)totalKills / totalRounds : 0;
            var damagePerRound = totalRounds > 0 ? (double)totalDamage / totalRounds : 0;
            var deathsPerRound = totalRounds > 0 ? (double)totalDeaths / totalRounds : 0;

            return new MVFormulaScore
            {
                HLTVRatingScore = avgHLTV,
                KillPerRoundScore = killPerRound,
                DamagePerRoundScore = damagePerRound,
                DeathsPerRoundScore = deathsPerRound,
                UtilityDamageScore = totalUtilityDamage,
                AWPkillsScore = totalAWPkills,
                FirstKillsScore = totalFirstKills,
                Commentator1Score = 0, // Manual input required
                Commentator2Score = 0, // Manual input required
                TotalScore = 0 // Will be calculated after ranking all players
            };
        }

        public async Task<List<PlayerMVPCandidate>> CalculateRankedMVPCandidatesAsync(int tournamentId, MVFormulaConfiguration formulaConfig)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return new List<PlayerMVPCandidate>();

            // Determine which matches to include based on tournament size
            var eligibleSeries = GetEligibleSeriesForMVP(tournament);
            var seriesIds = eligibleSeries.Select(s => s.Id).ToList();

            // Get all players who played in eligible matches
            var playerStats = await _context.PlayerStatsCS
                .Include(ps => ps.Match)
                    .ThenInclude(m => m.Series)
                .Where(ps => seriesIds.Contains(ps.Match.SeriesId))
                .ToListAsync();

            // Group by player UUID and calculate aggregate stats
            var playerGroups = playerStats.GroupBy(ps => ps.PlayerUUID);

            var candidates = new List<PlayerMVPCandidate>();

            foreach (var group in playerGroups)
            {
                var stats = group.ToList();
                var playerUUID = group.Key;

                // Get player from GameProfile using UUID
                var player = await GetPlayerByUUIDAsync(playerUUID);
                if (player == null) continue;

                // Get player's team from the tournament
                var team = await GetPlayerTeamInTournamentAsync(tournamentId, player.Id);
                if (team == null) continue;

                var formulaScore = CalculateFormulaScore(stats, formulaConfig);

                candidates.Add(new PlayerMVPCandidate
                {
                    PlayerId = player.Id,
                    PlayerName = player.Nickname,
                    TeamName = team.FullName,
                    TeamId = team.Id,
                    IsInEligibleMatches = true,
                    FormulaScore = formulaScore
                });
            }

            // Now calculate ranked points - only top player in each category gets points
            var rankedCandidates = CalculateRankedPoints(candidates, formulaConfig);

            return rankedCandidates.OrderByDescending(c => c.FormulaScore.TotalScore).ToList();
        }

        public async Task<List<PlayerEVPCandidate>> CalculateRankedEVPCandidatesAsync(int tournamentId, MVFormulaConfiguration formulaConfig)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return new List<PlayerEVPCandidate>();

            // Determine which matches to include based on tournament size
            var eligibleSeries = GetEligibleSeriesForEVP(tournament);
            var seriesIds = eligibleSeries.Select(s => s.Id).ToList();

            // Get all players who played in eligible matches
            var playerStats = await _context.PlayerStatsCS
                .Include(ps => ps.Match)
                    .ThenInclude(m => m.Series)
                .Where(ps => seriesIds.Contains(ps.Match.SeriesId))
                .ToListAsync();

            // Group by player UUID and calculate aggregate stats
            var playerGroups = playerStats.GroupBy(ps => ps.PlayerUUID);

            var candidates = new List<PlayerEVPCandidate>();

            foreach (var group in playerGroups)
            {
                var stats = group.ToList();
                var playerUUID = group.Key;

                // Get player from GameProfile using UUID
                var player = await GetPlayerByUUIDAsync(playerUUID);
                if (player == null) continue;

                // Get player's team from the tournament
                var team = await GetPlayerTeamInTournamentAsync(tournamentId, player.Id);
                if (team == null) continue;

                var formulaScore = CalculateFormulaScore(stats, formulaConfig);

                candidates.Add(new PlayerEVPCandidate
                {
                    PlayerId = player.Id,
                    PlayerName = player.Nickname,
                    TeamName = team.FullName,
                    TeamId = team.Id,
                    IsInEligibleMatches = true,
                    FormulaScore = formulaScore
                });
            }

            // Now calculate ranked points - only top player in each category gets points
            var rankedCandidates = CalculateRankedPointsEVP(candidates, formulaConfig);

            return rankedCandidates.OrderByDescending(c => c.FormulaScore.TotalScore).ToList();
        }

        private List<PlayerMVPCandidate> CalculateRankedPoints(List<PlayerMVPCandidate> candidates, MVFormulaConfiguration config)
        {
            if (!candidates.Any()) return candidates;

            // Award points only to the top player in each category
            var hltvWinner = candidates.OrderByDescending(c => c.FormulaScore.HLTVRatingScore).First();
            var killPerRoundWinner = candidates.OrderByDescending(c => c.FormulaScore.KillPerRoundScore).First();
            var damagePerRoundWinner = candidates.OrderByDescending(c => c.FormulaScore.DamagePerRoundScore).First();
            var deathsPerRoundWinner = candidates.OrderByDescending(c => c.FormulaScore.DeathsPerRoundScore).First();
            var utilityDamageWinner = candidates.OrderByDescending(c => c.FormulaScore.UtilityDamageScore).First();
            var awpKillsWinner = candidates.OrderByDescending(c => c.FormulaScore.AWPkillsScore).First();
            var firstKillsWinner = candidates.OrderByDescending(c => c.FormulaScore.FirstKillsScore).First();

            // Reset all scores to 0 first
            foreach (var candidate in candidates)
            {
                candidate.FormulaScore.HLTVRatingScore = 0;
                candidate.FormulaScore.KillPerRoundScore = 0;
                candidate.FormulaScore.DamagePerRoundScore = 0;
                candidate.FormulaScore.DeathsPerRoundScore = 0;
                candidate.FormulaScore.UtilityDamageScore = 0;
                candidate.FormulaScore.AWPkillsScore = 0;
                candidate.FormulaScore.FirstKillsScore = 0;
                candidate.FormulaScore.Commentator1Score = 0;
                candidate.FormulaScore.Commentator2Score = 0;
                candidate.FormulaScore.TotalScore = 0;
            }

            // Award points to winners
            hltvWinner.FormulaScore.HLTVRatingScore = config.HLTVRatingPoints;
            killPerRoundWinner.FormulaScore.KillPerRoundScore = config.KillPerRoundPoints;
            damagePerRoundWinner.FormulaScore.DamagePerRoundScore = config.DamagePerRoundPoints;
            deathsPerRoundWinner.FormulaScore.DeathsPerRoundScore = config.DeathsPerRoundPoints;
            utilityDamageWinner.FormulaScore.UtilityDamageScore = config.UtilityDamagePoints;
            awpKillsWinner.FormulaScore.AWPkillsScore = config.AWPkillsPoints;
            firstKillsWinner.FormulaScore.FirstKillsScore = config.FirstKillsPoints;

            // Award commentator points if selected
            if (config.Commentator1SelectedPlayerId.HasValue)
            {
                var commentator1Player = candidates.FirstOrDefault(c => c.PlayerId == config.Commentator1SelectedPlayerId.Value);
                if (commentator1Player != null)
                {
                    commentator1Player.FormulaScore.Commentator1Score = config.Commentator1Points;
                }
            }

            if (config.Commentator2SelectedPlayerId.HasValue)
            {
                var commentator2Player = candidates.FirstOrDefault(c => c.PlayerId == config.Commentator2SelectedPlayerId.Value);
                if (commentator2Player != null)
                {
                    commentator2Player.FormulaScore.Commentator2Score = config.Commentator2Points;
                }
            }

            // Calculate total scores
            foreach (var candidate in candidates)
            {
                candidate.FormulaScore.TotalScore = 
                    candidate.FormulaScore.HLTVRatingScore +
                    candidate.FormulaScore.KillPerRoundScore +
                    candidate.FormulaScore.DamagePerRoundScore +
                    candidate.FormulaScore.DeathsPerRoundScore +
                    candidate.FormulaScore.UtilityDamageScore +
                    candidate.FormulaScore.AWPkillsScore +
                    candidate.FormulaScore.FirstKillsScore +
                    candidate.FormulaScore.Commentator1Score +
                    candidate.FormulaScore.Commentator2Score;
            }

            return candidates;
        }

        private List<PlayerEVPCandidate> CalculateRankedPointsEVP(List<PlayerEVPCandidate> candidates, MVFormulaConfiguration config)
        {
            if (!candidates.Any()) return candidates;

            // Award points only to the top player in each category
            var hltvWinner = candidates.OrderByDescending(c => c.FormulaScore.HLTVRatingScore).First();
            var killPerRoundWinner = candidates.OrderByDescending(c => c.FormulaScore.KillPerRoundScore).First();
            var damagePerRoundWinner = candidates.OrderByDescending(c => c.FormulaScore.DamagePerRoundScore).First();
            var deathsPerRoundWinner = candidates.OrderByDescending(c => c.FormulaScore.DeathsPerRoundScore).First();
            var utilityDamageWinner = candidates.OrderByDescending(c => c.FormulaScore.UtilityDamageScore).First();
            var awpKillsWinner = candidates.OrderByDescending(c => c.FormulaScore.AWPkillsScore).First();
            var firstKillsWinner = candidates.OrderByDescending(c => c.FormulaScore.FirstKillsScore).First();

            // Reset all scores to 0 first
            foreach (var candidate in candidates)
            {
                candidate.FormulaScore.HLTVRatingScore = 0;
                candidate.FormulaScore.KillPerRoundScore = 0;
                candidate.FormulaScore.DamagePerRoundScore = 0;
                candidate.FormulaScore.DeathsPerRoundScore = 0;
                candidate.FormulaScore.UtilityDamageScore = 0;
                candidate.FormulaScore.AWPkillsScore = 0;
                candidate.FormulaScore.FirstKillsScore = 0;
                candidate.FormulaScore.Commentator1Score = 0;
                candidate.FormulaScore.Commentator2Score = 0;
                candidate.FormulaScore.TotalScore = 0;
            }

            // Award points to winners
            hltvWinner.FormulaScore.HLTVRatingScore = config.HLTVRatingPoints;
            killPerRoundWinner.FormulaScore.KillPerRoundScore = config.KillPerRoundPoints;
            damagePerRoundWinner.FormulaScore.DamagePerRoundScore = config.DamagePerRoundPoints;
            deathsPerRoundWinner.FormulaScore.DeathsPerRoundScore = config.DeathsPerRoundPoints;
            utilityDamageWinner.FormulaScore.UtilityDamageScore = config.UtilityDamagePoints;
            awpKillsWinner.FormulaScore.AWPkillsScore = config.AWPkillsPoints;
            firstKillsWinner.FormulaScore.FirstKillsScore = config.FirstKillsPoints;

            // Award commentator points if selected
            if (config.Commentator1SelectedPlayerId.HasValue)
            {
                var commentator1Player = candidates.FirstOrDefault(c => c.PlayerId == config.Commentator1SelectedPlayerId.Value);
                if (commentator1Player != null)
                {
                    commentator1Player.FormulaScore.Commentator1Score = config.Commentator1Points;
                }
            }

            if (config.Commentator2SelectedPlayerId.HasValue)
            {
                var commentator2Player = candidates.FirstOrDefault(c => c.PlayerId == config.Commentator2SelectedPlayerId.Value);
                if (commentator2Player != null)
                {
                    commentator2Player.FormulaScore.Commentator2Score = config.Commentator2Points;
                }
            }

            // Calculate total scores
            foreach (var candidate in candidates)
            {
                candidate.FormulaScore.TotalScore = 
                    candidate.FormulaScore.HLTVRatingScore +
                    candidate.FormulaScore.KillPerRoundScore +
                    candidate.FormulaScore.DamagePerRoundScore +
                    candidate.FormulaScore.DeathsPerRoundScore +
                    candidate.FormulaScore.UtilityDamageScore +
                    candidate.FormulaScore.AWPkillsScore +
                    candidate.FormulaScore.FirstKillsScore +
                    candidate.FormulaScore.Commentator1Score +
                    candidate.FormulaScore.Commentator2Score;
            }

            return candidates;
        }

        private async Task<Player> GetPlayerByUUIDAsync(string playerUUID)
        {
            var gameProfile = await _context.GameProfiles
                .Include(gp => gp.Player)
                .FirstOrDefaultAsync(gp => gp.UUID == playerUUID && gp.Provider == "FACEIT");

            return gameProfile?.Player;
        }

        private async Task<Team> GetPlayerTeamInTournamentAsync(int tournamentId, int playerId)
        {
            var tournamentTeam = await _context.TournamentTeams
                .Include(tt => tt.Team)
                .Include(tt => tt.Team.Transfers.Where(t => t.PlayerId == playerId && t.Status == PlayerTeamStatus.Active))
                .Where(tt => tt.TournamentId == tournamentId)
                .FirstOrDefaultAsync(tt => tt.Team.Transfers.Any(t => t.PlayerId == playerId && t.Status == PlayerTeamStatus.Active));

            return tournamentTeam?.Team;
        }

        public async Task<List<Team>> GetEligibleTeamsForEVP(int tournamentId, List<int> selectedPlayerIds)
        {
            var teams = await _context.TournamentTeams
                .Include(tt => tt.Team)
                .Where(tt => tt.TournamentId == tournamentId)
                .Select(tt => tt.Team)
                .ToListAsync();

            // Filter teams that have selected players
            var eligibleTeams = teams.Where(t => 
                t.Transfers.Any(tr => selectedPlayerIds.Contains(tr.PlayerId) && tr.Status == PlayerTeamStatus.Active))
                .ToList();

            return eligibleTeams;
        }

        public bool ValidateEVPSelection(List<int> selectedEVPIds, List<PlayerEVPCandidate> candidates)
        {
            // Rule: One team cannot win more than 3 MVP+EVP awards
            var teamCounts = new Dictionary<int, int>();
            
            foreach (var evpId in selectedEVPIds)
            {
                var candidate = candidates.FirstOrDefault(c => c.PlayerId == evpId);
                if (candidate != null)
                {
                    teamCounts[candidate.TeamId] = teamCounts.GetValueOrDefault(candidate.TeamId, 0) + 1;
                }
            }

            // Check if any team has more than 3 awards
            return teamCounts.Values.All(count => count <= 3);
        }
    }
}
