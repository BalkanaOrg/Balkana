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
            var tournament = await LoadTournamentForMvpEvpAsync(tournamentId);

            if (tournament == null) return new List<PlayerMVPCandidate>();

            if (IsLeagueOfLegendsTournament(tournament))
            {
                var mvpLol = await BuildLolMvpCandidatesAsync(
                    tournamentId, GetEligibleSeriesForMVP(tournament), formulaConfig);
                return mvpLol
                    .OrderByDescending(p => p.FormulaScore.LolDpm)
                    .ThenByDescending(p => p.FormulaScore.LolKdaValue)
                    .ToList();
            }

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
                var player = await GetPlayerByFaceitUuidAsync(playerUUID);
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
            var tournament = await LoadTournamentForMvpEvpAsync(tournamentId);

            if (tournament == null) return new List<PlayerEVPCandidate>();

            if (IsLeagueOfLegendsTournament(tournament))
            {
                var evpLol = await BuildLolEvpCandidatesAsync(
                    tournamentId, GetEligibleSeriesForEVP(tournament), formulaConfig);
                return evpLol
                    .OrderByDescending(p => p.FormulaScore.LolDpm)
                    .ThenByDescending(p => p.FormulaScore.LolKdaValue)
                    .ToList();
            }

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
                var player = await GetPlayerByFaceitUuidAsync(playerUUID);
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

        private async Task<Tournament?> LoadTournamentForMvpEvpAsync(int tournamentId)
        {
            return await _context.Tournaments
                .AsSplitQuery()
                .Include(t => t.Game)
                .Include(t => t.Series)
                .Include(t => t.TournamentTeams)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
        }

        private static bool IsLeagueOfLegendsTournament(Tournament? tournament) =>
            tournament != null && (tournament.GameId == 2
                || string.Equals(tournament.Game?.ShortName, "LoL", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// CS2: finals only if &amp;lt; 16 teams; semis+finals if 16+.
        /// LoL: finals only if ≤8 teams; semis+finals if 9+.
        /// </summary>
        private List<Balkana.Data.Models.Series> GetEligibleSeriesForMVP(Tournament tournament)
        {
            var allSeries = tournament.Series.ToList();
            var teamCount = tournament.TournamentTeams.Count;

            if (IsLeagueOfLegendsTournament(tournament))
            {
                if (teamCount > 8)
                    return GetFinalAndSemiFinalSeries(allSeries);
                return GetFinalSeries(allSeries);
            }

            if (teamCount >= 16)
                return GetFinalAndSemiFinalSeries(allSeries);

            return GetFinalSeries(allSeries);
        }

        /// <summary>
        /// Formula EVP pool: semifinals + finals for all tournament sizes (including 16+).
        /// </summary>
        private List<Balkana.Data.Models.Series> GetEligibleSeriesForEVP(Tournament tournament)
        {
            var allSeries = tournament.Series.ToList();
            return GetFinalAndSemiFinalSeries(allSeries);
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

        private MVFormulaScore CalculateFormulaScore(List<PlayerStatistic_CS2> stats, MVFormulaConfiguration config)
        {
            if (!stats.Any()) return new MVFormulaScore();

            var totalRounds = stats.Sum(s => s.RoundsPlayed);
            var totalKills = stats.Sum(s => s.Kills);
            var totalDeaths = stats.Sum(s => s.Deaths);
            var totalDamage = stats.Sum(s => s.Damage);
            var totalAssists = stats.Sum(s => s.Assists);
            var totalSniperKills = stats.Sum(s => s.SniperKills);
            var totalFirstKills = stats.Sum(s => s.FK);
            var totalUtilityUsage = stats.Sum(s => s.UtilityUsage);

            var avgHLTV = stats.Average(s => s.HLTV1);
            var killPerRound = totalRounds > 0 ? (double)totalKills / totalRounds : 0;
            var assistsPerRound = totalRounds > 0 ? (double)totalAssists / totalRounds : 0;
            var damagePerRound = totalRounds > 0 ? (double)totalDamage / totalRounds : 0;
            var deathsPerRound = totalRounds > 0 ? (double)totalDeaths / totalRounds : 0;
            var utilityScorePerRound = totalRounds > 0 ? (double)totalUtilityUsage / totalRounds : 0;

            // Calculate Impact: 2.13*KPR - 0.42*Assists per round - 0.41
            var impact = 2.13 * killPerRound - 0.42 * assistsPerRound - 0.41;

            return new MVFormulaScore
            {
                HLTVRatingScore = avgHLTV,
                KillPerRoundScore = killPerRound,
                ImpactScore = impact,
                UtilityScore = utilityScorePerRound,
                SniperScore = totalSniperKills,
                ADRScore = damagePerRound,
                DPRScore = deathsPerRound,
                FirstKillsScore = totalFirstKills,
                Commentator1Score = 0, // Manual input required
                Commentator2Score = 0, // Manual input required
                TotalScore = 0 // Will be calculated after ranking all players
            };
        }

        private static MVFormulaScore CalculateLolRawScore(List<PlayerStatistic_LoL> rowStats, MVFormulaConfiguration _config)
        {
            if (!rowStats.Any()) return new MVFormulaScore();

            var k = 0;
            var a = 0;
            var d = 0;
            var vision = 0;
            var cs = 0;
            var dmg = 0;
            var objDmg = 0;
            var totalGameMinutes = 0.0;

            foreach (var s in rowStats)
            {
                k += s.Kills ?? 0;
                a += s.Assists ?? 0;
                d += s.Deaths ?? 0;
                vision += s.VisionScore;
                cs += s.CreepScore;
                dmg += s.TotalDamageToChampions ?? 0;
                objDmg += s.TotalDamageToObjectives ?? 0;
                if (s.Match is MatchLoL m)
                    totalGameMinutes += m.GameDurationSeconds / 60.0;
            }

            var games = rowStats.GroupBy(s => s.MatchId).Count();
            if (games < 1) games = 1;
            var kdaVal = d == 0 ? 1_000_000.0 + k + a : (k + a) / (double)d;
            var dpm = totalGameMinutes > 0.01 ? dmg / totalGameMinutes : 0;
            var avgCs = cs / (double)games;
            var avgObj = objDmg / (double)games;

            return new MVFormulaScore
            {
                LolKdaValue = kdaVal,
                LolDpm = dpm,
                LolVisionTotal = vision,
                LolAverageCs = avgCs,
                LolAverageObjectivesDmg = avgObj,
                Commentator1Score = 0,
                Commentator2Score = 0,
                TotalScore = 0
            };
        }

        private async Task<List<PlayerMVPCandidate>> BuildLolMvpCandidatesAsync(
            int tournamentId,
            List<Balkana.Data.Models.Series> eligibleSeries,
            MVFormulaConfiguration formulaConfig)
        {
            return await BuildLolPlayerListAsync<PlayerMVPCandidate>(tournamentId, eligibleSeries, formulaConfig, (a, t, s, c) => new PlayerMVPCandidate
            {
                PlayerId = a.Id,
                PlayerName = a.Nickname,
                TeamName = t.FullName,
                TeamId = t.Id,
                IsInEligibleMatches = true,
                FormulaScore = c
            });
        }

        private async Task<List<PlayerEVPCandidate>> BuildLolEvpCandidatesAsync(
            int tournamentId,
            List<Balkana.Data.Models.Series> eligibleSeries,
            MVFormulaConfiguration formulaConfig)
        {
            return await BuildLolPlayerListAsync<PlayerEVPCandidate>(tournamentId, eligibleSeries, formulaConfig, (a, t, s, c) => new PlayerEVPCandidate
            {
                PlayerId = a.Id,
                PlayerName = a.Nickname,
                TeamName = t.FullName,
                TeamId = t.Id,
                IsInEligibleMatches = true,
                FormulaScore = c
            });
        }

        private async Task<List<TC>> BuildLolPlayerListAsync<TC>(
            int tournamentId,
            List<Balkana.Data.Models.Series> eligibleSeries,
            MVFormulaConfiguration formulaConfig,
            Func<Player, Team, List<PlayerStatistic_LoL>, MVFormulaScore, TC> build)
        {
            var seriesIds = eligibleSeries.Select(s => s.Id).ToList();
            if (seriesIds.Count == 0)
                return new List<TC>();

            var playerStats = await _context.PlayerStatsLoL
                .Include(ps => ps.Match)
                    .ThenInclude(m => m!.Series)
                .Where(ps => seriesIds.Contains(ps.Match.SeriesId) && ps.Source == "RIOT")
                .ToListAsync();

            var outList = new List<TC>();
            var groups = playerStats.GroupBy(ps => ps.PlayerUUID);
            foreach (var group in groups)
            {
                var rowStats = group.ToList();
                var player = await GetPlayerByRiotPuuidAsync(group.Key);
                if (player == null) continue;
                var team = await GetPlayerTeamInTournamentAsync(tournamentId, player.Id);
                if (team == null) continue;
                var raw = CalculateLolRawScore(rowStats, formulaConfig);
                outList.Add(build(player, team, rowStats, raw));
            }

            return outList;
        }

        public async Task<List<PlayerMVPCandidate>> CalculateRankedMVPCandidatesAsync(int tournamentId, MVFormulaConfiguration formulaConfig)
        {
            var tournament = await LoadTournamentForMvpEvpAsync(tournamentId);

            if (tournament == null) return new List<PlayerMVPCandidate>();

            if (IsLeagueOfLegendsTournament(tournament))
            {
                var lolMvp = await BuildLolMvpCandidatesAsync(
                    tournamentId, GetEligibleSeriesForMVP(tournament), formulaConfig);
                return CalculateRankedPointsLolMvp(lolMvp, formulaConfig)
                    .OrderByDescending(c => c.FormulaScore.TotalScore)
                    .ToList();
            }

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
                var player = await GetPlayerByFaceitUuidAsync(playerUUID);
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
            var tournament = await LoadTournamentForMvpEvpAsync(tournamentId);

            if (tournament == null) return new List<PlayerEVPCandidate>();

            if (IsLeagueOfLegendsTournament(tournament))
            {
                var lolEvp = await BuildLolEvpCandidatesAsync(
                    tournamentId, GetEligibleSeriesForEVP(tournament), formulaConfig);
                return CalculateRankedPointsLolEvp(lolEvp, formulaConfig)
                    .OrderByDescending(c => c.FormulaScore.TotalScore)
                    .ToList();
            }

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
                var player = await GetPlayerByFaceitUuidAsync(playerUUID);
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

        private const int LolKdaCategoryPoints = 5;
        private const int LolDpmCategoryPoints = 5;
        private const int LolVisionCategoryPoints = 5;
        private const int LolCsCategoryPoints = 5;
        private const int LolObjectivesCategoryPoints = 3;

        private static List<PlayerMVPCandidate> CalculateRankedPointsLolMvp(
            List<PlayerMVPCandidate> candidates, MVFormulaConfiguration config)
        {
            if (candidates.Count == 0) return candidates;

            var kda = candidates.OrderByDescending(c => c.FormulaScore.LolKdaValue).First();
            var dpm = candidates.OrderByDescending(c => c.FormulaScore.LolDpm).First();
            var vis = candidates.OrderByDescending(c => c.FormulaScore.LolVisionTotal).First();
            var csx = candidates.OrderByDescending(c => c.FormulaScore.LolAverageCs).First();
            var obj = candidates.OrderByDescending(c => c.FormulaScore.LolAverageObjectivesDmg).First();

            foreach (var c in candidates)
            {
                c.FormulaScore.LolKdaPoints = 0;
                c.FormulaScore.LolDpmPoints = 0;
                c.FormulaScore.LolVisionPoints = 0;
                c.FormulaScore.LolCsPoints = 0;
                c.FormulaScore.LolObjectivesPoints = 0;
                c.FormulaScore.Commentator1Score = 0;
                c.FormulaScore.Commentator2Score = 0;
                c.FormulaScore.TotalScore = 0;
            }

            kda.FormulaScore.LolKdaPoints = LolKdaCategoryPoints;
            dpm.FormulaScore.LolDpmPoints = LolDpmCategoryPoints;
            vis.FormulaScore.LolVisionPoints = LolVisionCategoryPoints;
            csx.FormulaScore.LolCsPoints = LolCsCategoryPoints;
            obj.FormulaScore.LolObjectivesPoints = LolObjectivesCategoryPoints;

            if (config.Commentator1SelectedPlayerId.HasValue)
            {
                var a = candidates.FirstOrDefault(p => p.PlayerId == config.Commentator1SelectedPlayerId.Value);
                if (a != null) a.FormulaScore.Commentator1Score = config.Commentator1Points;
            }
            if (config.Commentator2SelectedPlayerId.HasValue)
            {
                var a = candidates.FirstOrDefault(p => p.PlayerId == config.Commentator2SelectedPlayerId.Value);
                if (a != null) a.FormulaScore.Commentator2Score = config.Commentator2Points;
            }

            foreach (var c in candidates)
            {
                c.FormulaScore.TotalScore = c.FormulaScore.LolKdaPoints
                    + c.FormulaScore.LolDpmPoints
                    + c.FormulaScore.LolVisionPoints
                    + c.FormulaScore.LolCsPoints
                    + c.FormulaScore.LolObjectivesPoints
                    + c.FormulaScore.Commentator1Score
                    + c.FormulaScore.Commentator2Score;
            }

            return candidates;
        }

        private static List<PlayerEVPCandidate> CalculateRankedPointsLolEvp(
            List<PlayerEVPCandidate> candidates, MVFormulaConfiguration config)
        {
            if (candidates.Count == 0) return candidates;

            var kda = candidates.OrderByDescending(c => c.FormulaScore.LolKdaValue).First();
            var dpm = candidates.OrderByDescending(c => c.FormulaScore.LolDpm).First();
            var vis = candidates.OrderByDescending(c => c.FormulaScore.LolVisionTotal).First();
            var csx = candidates.OrderByDescending(c => c.FormulaScore.LolAverageCs).First();
            var obj = candidates.OrderByDescending(c => c.FormulaScore.LolAverageObjectivesDmg).First();

            foreach (var c in candidates)
            {
                c.FormulaScore.LolKdaPoints = 0;
                c.FormulaScore.LolDpmPoints = 0;
                c.FormulaScore.LolVisionPoints = 0;
                c.FormulaScore.LolCsPoints = 0;
                c.FormulaScore.LolObjectivesPoints = 0;
                c.FormulaScore.Commentator1Score = 0;
                c.FormulaScore.Commentator2Score = 0;
                c.FormulaScore.TotalScore = 0;
            }

            kda.FormulaScore.LolKdaPoints = LolKdaCategoryPoints;
            dpm.FormulaScore.LolDpmPoints = LolDpmCategoryPoints;
            vis.FormulaScore.LolVisionPoints = LolVisionCategoryPoints;
            csx.FormulaScore.LolCsPoints = LolCsCategoryPoints;
            obj.FormulaScore.LolObjectivesPoints = LolObjectivesCategoryPoints;

            if (config.Commentator1SelectedPlayerId.HasValue)
            {
                var a = candidates.FirstOrDefault(p => p.PlayerId == config.Commentator1SelectedPlayerId.Value);
                if (a != null) a.FormulaScore.Commentator1Score = config.Commentator1Points;
            }
            if (config.Commentator2SelectedPlayerId.HasValue)
            {
                var a = candidates.FirstOrDefault(p => p.PlayerId == config.Commentator2SelectedPlayerId.Value);
                if (a != null) a.FormulaScore.Commentator2Score = config.Commentator2Points;
            }

            foreach (var c in candidates)
            {
                c.FormulaScore.TotalScore = c.FormulaScore.LolKdaPoints
                    + c.FormulaScore.LolDpmPoints
                    + c.FormulaScore.LolVisionPoints
                    + c.FormulaScore.LolCsPoints
                    + c.FormulaScore.LolObjectivesPoints
                    + c.FormulaScore.Commentator1Score
                    + c.FormulaScore.Commentator2Score;
            }

            return candidates;
        }

        private List<PlayerMVPCandidate> CalculateRankedPoints(List<PlayerMVPCandidate> candidates, MVFormulaConfiguration config)
        {
            if (!candidates.Any()) return candidates;

            // Award points only to the top player in each category
            var hltvWinner = candidates.OrderByDescending(c => c.FormulaScore.HLTVRatingScore).First();
            var killPerRoundWinner = candidates.OrderByDescending(c => c.FormulaScore.KillPerRoundScore).First();
            var impactWinner = candidates.OrderByDescending(c => c.FormulaScore.ImpactScore).First();
            var utilityScoreWinner = candidates.OrderByDescending(c => c.FormulaScore.UtilityScore).First();
            var firstKillsWinner = candidates.OrderByDescending(c => c.FormulaScore.FirstKillsScore).First();
            var sniperWinner = candidates.OrderByDescending(c => c.FormulaScore.SniperScore).First();
            var adrWinner = candidates.OrderByDescending(c => c.FormulaScore.ADRScore).First();
            var dprWinner = candidates.OrderBy(c => c.FormulaScore.DPRScore).First(); // Lowest DPR wins

            // Reset all scores to 0 first
            foreach (var candidate in candidates)
            {
                candidate.FormulaScore.HLTVRatingScore = 0;
                candidate.FormulaScore.KillPerRoundScore = 0;
                candidate.FormulaScore.ImpactScore = 0;
                candidate.FormulaScore.UtilityScore = 0;
                candidate.FormulaScore.FirstKillsScore = 0;
                candidate.FormulaScore.SniperScore = 0;
                candidate.FormulaScore.ADRScore = 0;
                candidate.FormulaScore.DPRScore = 0;
                candidate.FormulaScore.Commentator1Score = 0;
                candidate.FormulaScore.Commentator2Score = 0;
                candidate.FormulaScore.TotalScore = 0;
            }

            // Award points to winners
            hltvWinner.FormulaScore.HLTVRatingScore = config.HLTVRatingPoints;
            killPerRoundWinner.FormulaScore.KillPerRoundScore = config.KillPerRoundPoints;
            impactWinner.FormulaScore.ImpactScore = config.ImpactPoints;
            utilityScoreWinner.FormulaScore.UtilityScore = config.UtilityScorePoints;
            firstKillsWinner.FormulaScore.FirstKillsScore = config.FirstKillsPoints;
            sniperWinner.FormulaScore.SniperScore = config.SniperPoints;
            adrWinner.FormulaScore.ADRScore = config.ADRPoints;
            dprWinner.FormulaScore.DPRScore = config.DPRPoints;

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
                    candidate.FormulaScore.ImpactScore +
                    candidate.FormulaScore.UtilityScore +
                    candidate.FormulaScore.FirstKillsScore +
                    candidate.FormulaScore.SniperScore +
                    candidate.FormulaScore.ADRScore +
                    candidate.FormulaScore.DPRScore +
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
            var impactWinner = candidates.OrderByDescending(c => c.FormulaScore.ImpactScore).First();
            var utilityScoreWinner = candidates.OrderByDescending(c => c.FormulaScore.UtilityScore).First();
            var firstKillsWinner = candidates.OrderByDescending(c => c.FormulaScore.FirstKillsScore).First();
            var sniperWinner = candidates.OrderByDescending(c => c.FormulaScore.SniperScore).First();
            var adrWinner = candidates.OrderByDescending(c => c.FormulaScore.ADRScore).First();
            var dprWinner = candidates.OrderBy(c => c.FormulaScore.DPRScore).First(); // Lowest DPR wins

            // Reset all scores to 0 first
            foreach (var candidate in candidates)
            {
                candidate.FormulaScore.HLTVRatingScore = 0;
                candidate.FormulaScore.KillPerRoundScore = 0;
                candidate.FormulaScore.ImpactScore = 0;
                candidate.FormulaScore.UtilityScore = 0;
                candidate.FormulaScore.FirstKillsScore = 0;
                candidate.FormulaScore.SniperScore = 0;
                candidate.FormulaScore.ADRScore = 0;
                candidate.FormulaScore.DPRScore = 0;
                candidate.FormulaScore.Commentator1Score = 0;
                candidate.FormulaScore.Commentator2Score = 0;
                candidate.FormulaScore.TotalScore = 0;
            }

            // Award points to winners
            hltvWinner.FormulaScore.HLTVRatingScore = config.HLTVRatingPoints;
            killPerRoundWinner.FormulaScore.KillPerRoundScore = config.KillPerRoundPoints;
            impactWinner.FormulaScore.ImpactScore = config.ImpactPoints;
            utilityScoreWinner.FormulaScore.UtilityScore = config.UtilityScorePoints;
            firstKillsWinner.FormulaScore.FirstKillsScore = config.FirstKillsPoints;
            sniperWinner.FormulaScore.SniperScore = config.SniperPoints;
            adrWinner.FormulaScore.ADRScore = config.ADRPoints;
            dprWinner.FormulaScore.DPRScore = config.DPRPoints;

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
                    candidate.FormulaScore.ImpactScore +
                    candidate.FormulaScore.UtilityScore +
                    candidate.FormulaScore.FirstKillsScore +
                    candidate.FormulaScore.SniperScore +
                    candidate.FormulaScore.ADRScore +
                    candidate.FormulaScore.DPRScore +
                    candidate.FormulaScore.Commentator1Score +
                    candidate.FormulaScore.Commentator2Score;
            }

            return candidates;
        }

        private static async Task<Player?> GetPlayerByFaceitUuidAsync(
            ApplicationDbContext context, string playerUUID) =>
            await GetPlayerByProviderAsync(context, playerUUID, "FACEIT");

        private static async Task<Player?> GetPlayerByRiotPuuidAsync(
            ApplicationDbContext context, string puuid) =>
            await GetPlayerByProviderAsync(context, puuid, "RIOT");

        private static async Task<Player?> GetPlayerByProviderAsync(
            ApplicationDbContext context, string u, string provider)
        {
            var gameProfile = await context.GameProfiles
                .Include(gp => gp.Player)
                .FirstOrDefaultAsync(gp => gp.UUID == u && gp.Provider == provider);

            return gameProfile?.Player;
        }

        private Task<Player?> GetPlayerByFaceitUuidAsync(string playerUUID) =>
            GetPlayerByFaceitUuidAsync(_context, playerUUID);

        private Task<Player?> GetPlayerByRiotPuuidAsync(string puuid) =>
            GetPlayerByRiotPuuidAsync(_context, puuid);

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
