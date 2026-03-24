using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Services.Matches.Models;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Tournaments
{
    public class RiotPendingMatchImportService : IRiotPendingMatchImportService
    {
        private readonly ApplicationDbContext _db;
        private readonly RiotMatchImporter _importer;
        private readonly IRiotTournamentService _riotService;

        public RiotPendingMatchImportService(
            ApplicationDbContext db,
            RiotMatchImporter importer,
            IRiotTournamentService riotService)
        {
            _db = db;
            _importer = importer;
            _riotService = riotService;
        }

        public async Task<(bool Success, string? Error)> ImportAsync(int pendingMatchId, int seriesId)
        {
            var pending = await _db.RiotPendingMatches
                .Include(p => p.RiotTournamentCode)
                .FirstOrDefaultAsync(p => p.Id == pendingMatchId);

            if (pending == null)
                return (false, "Pending match not found");

            if (pending.Status != RiotPendingMatchStatus.Pending)
                return (false, $"Match already {pending.Status}");

            if (pending.MatchId == "UNKNOWN")
                return (false, "Cannot import: matchId could not be derived from callback");

            var series = await _db.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Matches)
                .FirstOrDefaultAsync(s => s.Id == seriesId);

            if (series == null)
                return (false, "Series not found");

            var matches = await _importer.ImportMatchAsync(pending.MatchId, _db);
            if (matches == null || !matches.Any())
                return (false, "Failed to fetch match data from Riot API");

            foreach (var match in matches)
            {
                match.SeriesId = seriesId;
                _db.Matches.Add(match);
            }

            await _db.SaveChangesAsync();

            await UpdateSeriesWinnerAsync(series, matches);

            await AdvanceWinnerToNextSeriesAsync(series);

            if (pending.RiotTournamentCode != null && !string.IsNullOrEmpty(pending.RiotTournamentCode.Code))
            {
                try
                {
                    var firstMatch = matches.First();
                    await _riotService.UpdateTournamentCodeWithMatchAsync(
                        pending.RiotTournamentCode.Code,
                        pending.MatchId,
                        firstMatch.Id);
                }
                catch
                {
                    // Non-fatal - match is imported
                }
            }

            pending.Status = RiotPendingMatchStatus.Imported;
            pending.ImportedAt = DateTime.UtcNow;
            pending.SeriesId = seriesId;
            pending.ImportedMatchDbId = matches.First().Id;
            await _db.SaveChangesAsync();

            return (true, null);
        }

        private async Task UpdateSeriesWinnerAsync(Data.Models.Series series, List<Match> matches)
        {
            if (!matches.Any()) return;

            int teamAWins = 0, teamBWins = 0;
            int winsNeeded = (matches.Count / 2) + 1;

            foreach (var match in matches)
            {
                if (!match.IsCompleted) continue;
                var winner = DetermineMatchWinner(match);
                if (winner == series.TeamA) teamAWins++;
                else if (winner == series.TeamB) teamBWins++;
            }

            if (teamAWins >= winsNeeded || teamBWins >= winsNeeded)
            {
                series.isFinished = true;
                series.WinnerTeamId = teamAWins >= winsNeeded ? series.TeamAId : series.TeamBId;
            }

            await _db.SaveChangesAsync();
        }

        private static Team? DetermineMatchWinner(Match match)
        {
            if (match.WinnerTeam != null) return match.WinnerTeam;
            if (match is MatchCS csMatch)
            {
                var teamAStats = csMatch.PlayerStats.Where(ps => ps.Team == csMatch.TeamASourceSlot).OfType<PlayerStatistic_CS2>().ToList();
                var teamBStats = csMatch.PlayerStats.Where(ps => ps.Team == csMatch.TeamBSourceSlot).OfType<PlayerStatistic_CS2>().ToList();
                if (teamAStats.Any() && teamBStats.Any())
                {
                    var teamARounds = teamAStats.FirstOrDefault()?.RoundsPlayed ?? 0;
                    var teamBRounds = teamBStats.FirstOrDefault()?.RoundsPlayed ?? 0;
                    if (teamARounds > teamBRounds) return match.TeamA;
                    if (teamBRounds > teamARounds) return match.TeamB;
                }
            }
            return null;
        }

        private async Task AdvanceWinnerToNextSeriesAsync(Data.Models.Series currentSeries)
        {
            if (!currentSeries.isFinished) return;

            Data.Models.Series? nextSeries = null;
            if (currentSeries.NextSeriesId != null)
                nextSeries = await _db.Series.Include(s => s.TeamA).Include(s => s.TeamB)
                    .FirstOrDefaultAsync(s => s.Id == currentSeries.NextSeriesId);

            if (nextSeries == null)
                nextSeries = await FindNextSeriesDynamicallyAsync(currentSeries);

            if (nextSeries == null) return;

            var winner = await _db.Teams.FindAsync(currentSeries.WinnerTeamId);
            if (winner == null) return;

            if (nextSeries.TeamAId == null)
            {
                nextSeries.TeamAId = winner.Id;
                nextSeries.TeamA = winner;
            }
            else if (nextSeries.TeamBId == null)
            {
                nextSeries.TeamBId = winner.Id;
                nextSeries.TeamB = winner;
            }

            await _db.SaveChangesAsync();
        }

        private async Task<Data.Models.Series?> FindNextSeriesDynamicallyAsync(Data.Models.Series currentSeries)
        {
            var nextRound = currentSeries.Round + 1;
            var next = await _db.Series.Include(s => s.TeamA).Include(s => s.TeamB)
                .Where(s => s.TournamentId == currentSeries.TournamentId &&
                            s.Round == nextRound &&
                            (s.TeamAId == null || s.TeamBId == null))
                .OrderBy(s => s.Position)
                .FirstOrDefaultAsync();

            if (next != null) return next;

            var grandFinal = await _db.Series.Include(s => s.TeamA).Include(s => s.TeamB)
                .Where(s => s.TournamentId == currentSeries.TournamentId &&
                            s.Bracket == BracketType.GrandFinal &&
                            (s.TeamAId == null || s.TeamBId == null))
                .FirstOrDefaultAsync();

            return grandFinal;
        }
    }
}
