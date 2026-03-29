using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Tournaments
{
    public class TournamentBracketPlacementService
    {
        private readonly ApplicationDbContext _context;

        public TournamentBracketPlacementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TournamentPlacement>> BuildPlacementsListAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
            if (tournament == null)
                return new List<TournamentPlacement>();

            var participatingTeams = await _context.TournamentTeams
                .Include(tt => tt.Team)
                .Where(tt => tt.TournamentId == tournamentId)
                .OrderBy(tt => tt.Seed)
                .Select(tt => tt.Team)
                .ToListAsync();

            var allSeries = await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.WinnerTeam)
                .Include(s => s.Matches)
                .Where(s => s.TournamentId == tournamentId)
                .ToListAsync();

            var placements = new List<TournamentPlacement>();

            if (tournament.Elimination == EliminationType.Single)
                await GenerateSingleEliminationPlacements(allSeries, participatingTeams, placements, tournament);
            else
                await GenerateDoubleEliminationPlacements(allSeries, participatingTeams, placements, tournament);

            return placements;
        }

        public async Task PersistPlacementsAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Placements)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
            if (tournament == null)
                return;

            _context.TournamentPlacements.RemoveRange(tournament.Placements);
            await _context.SaveChangesAsync();

            var newPlacements = await BuildPlacementsListAsync(tournamentId);
            foreach (var p in newPlacements)
                _context.TournamentPlacements.Add(p);

            await _context.SaveChangesAsync();
        }

        private static TournamentPlacement CreatePlacement(Tournament tournament, Team team, int placement)
        {
            return new TournamentPlacement
            {
                TournamentId = tournament.Id,
                TeamId = team.Id,
                Placement = placement,
                PointsAwarded = TournamentPlacementScoring.GetPointsForPlacement(tournament, placement),
                OrganisationPointsAwarded = 0
            };
        }

        private static Task GenerateSingleEliminationPlacements(
            List<Balkana.Data.Models.Series> allSeries,
            List<Team> participatingTeams,
            List<TournamentPlacement> placements,
            Tournament tournament)
        {
            var seriesByRound = allSeries
                .Where(s => s.Bracket == BracketType.Upper)
                .GroupBy(s => s.Round)
                .OrderByDescending(g => g.Key)
                .ToList();

            if (!seriesByRound.Any())
                return Task.CompletedTask;

            var eliminatedTeams = new Dictionary<int, List<Team>>();
            var remainingTeams = new HashSet<Team>(participatingTeams);

            foreach (var roundGroup in seriesByRound)
            {
                var roundEliminated = new List<Team>();

                foreach (var series in roundGroup)
                {
                    if (series.isFinished && series.TeamA != null && series.TeamB != null && series.WinnerTeam != null)
                    {
                        var winner = series.WinnerTeam;
                        var loser = series.TeamA == winner ? series.TeamB : series.TeamA;

                        if (loser != null && remainingTeams.Contains(loser))
                        {
                            roundEliminated.Add(loser);
                            remainingTeams.Remove(loser);
                        }
                    }
                }

                if (roundEliminated.Any())
                    eliminatedTeams[roundGroup.Key] = roundEliminated;
            }

            int currentPlacement = 1;

            if (remainingTeams.Count == 1)
            {
                var winner = remainingTeams.First();
                placements.Add(CreatePlacement(tournament, winner, currentPlacement));
                currentPlacement++;
            }

            foreach (var roundGroup in seriesByRound)
            {
                if (eliminatedTeams.TryGetValue(roundGroup.Key, out var roundEliminated))
                {
                    int teamsEliminated = roundEliminated.Count;

                    foreach (var team in roundEliminated)
                        placements.Add(CreatePlacement(tournament, team, currentPlacement));

                    currentPlacement += teamsEliminated;
                }
            }

            var placedTeamIds = placements.Select(p => p.TeamId).ToHashSet();
            var unplacedTeams = participatingTeams.Where(t => !placedTeamIds.Contains(t.Id)).ToList();

            foreach (var team in unplacedTeams)
                placements.Add(CreatePlacement(tournament, team, currentPlacement));

            return Task.CompletedTask;
        }

        private static Task GenerateDoubleEliminationPlacements(
            List<Balkana.Data.Models.Series> allSeries,
            List<Team> participatingTeams,
            List<TournamentPlacement> placements,
            Tournament tournament)
        {
            var placedTeamIds = new HashSet<int>();

            var grandFinal = allSeries
                .Where(s => s.Bracket == BracketType.GrandFinal)
                .FirstOrDefault();

            if (grandFinal?.isFinished == true && grandFinal.WinnerTeam != null)
            {
                if (!placedTeamIds.Contains(grandFinal.WinnerTeam.Id))
                {
                    placements.Add(CreatePlacement(tournament, grandFinal.WinnerTeam, 1));
                    placedTeamIds.Add(grandFinal.WinnerTeam.Id);
                }

                var runnerUp = grandFinal.TeamA == grandFinal.WinnerTeam ? grandFinal.TeamB : grandFinal.TeamA;
                if (runnerUp != null && !placedTeamIds.Contains(runnerUp.Id))
                {
                    placements.Add(CreatePlacement(tournament, runnerUp, 2));
                    placedTeamIds.Add(runnerUp.Id);
                }

                var upperBracketFinal = allSeries
                    .Where(s => s.Bracket == BracketType.Upper)
                    .OrderByDescending(s => s.Round)
                    .FirstOrDefault();

                if (upperBracketFinal?.isFinished == true)
                {
                    var upperBracketLoser = upperBracketFinal.TeamA == grandFinal.WinnerTeam
                        ? upperBracketFinal.TeamB
                        : upperBracketFinal.TeamA;
                    if (upperBracketLoser != null && upperBracketLoser.Id != runnerUp?.Id &&
                        !placedTeamIds.Contains(upperBracketLoser.Id))
                    {
                        placements.Add(CreatePlacement(tournament, upperBracketLoser, 3));
                        placedTeamIds.Add(upperBracketLoser.Id);
                    }
                }

                var eliminatedTeams = new Dictionary<int, List<Team>>();

                var upperBracketSeries = allSeries
                    .Where(s => s.Bracket == BracketType.Upper && s.isFinished)
                    .OrderByDescending(s => s.Round)
                    .ThenByDescending(s => s.Position)
                    .ToList();

                foreach (var series in upperBracketSeries)
                {
                    if (series.WinnerTeam != null)
                    {
                        var loser = series.TeamA == series.WinnerTeam ? series.TeamB : series.TeamA;
                        if (loser != null && !placedTeamIds.Contains(loser.Id))
                        {
                            if (series.Round == upperBracketFinal?.Round)
                            {
                                placements.Add(CreatePlacement(tournament, loser, 4));
                                placedTeamIds.Add(loser.Id);
                            }
                            else
                            {
                                if (!eliminatedTeams.ContainsKey(series.Round))
                                    eliminatedTeams[series.Round] = new List<Team>();
                                if (!eliminatedTeams[series.Round].Any(t => t.Id == loser.Id))
                                    eliminatedTeams[series.Round].Add(loser);
                            }
                        }
                    }
                }

                var lowerBracketSeries = allSeries
                    .Where(s => s.Bracket == BracketType.Lower && s.isFinished)
                    .OrderByDescending(s => s.Round)
                    .ThenByDescending(s => s.Position)
                    .ToList();

                foreach (var series in lowerBracketSeries)
                {
                    if (series.WinnerTeam != null)
                    {
                        var loser = series.TeamA == series.WinnerTeam ? series.TeamB : series.TeamA;
                        if (loser != null && !placedTeamIds.Contains(loser.Id))
                        {
                            var key = series.Round + 100;
                            if (!eliminatedTeams.ContainsKey(key))
                                eliminatedTeams[key] = new List<Team>();
                            if (!eliminatedTeams[key].Any(t => t.Id == loser.Id))
                                eliminatedTeams[key].Add(loser);
                        }
                    }
                }

                int currentPlacement = 5;
                foreach (var elimination in eliminatedTeams.OrderByDescending(kvp => kvp.Key))
                {
                    var teamsInThisElimination = elimination.Value.Where(t => !placedTeamIds.Contains(t.Id)).ToList();
                    foreach (var team in teamsInThisElimination)
                    {
                        placements.Add(CreatePlacement(tournament, team, currentPlacement));
                        placedTeamIds.Add(team.Id);
                    }

                    currentPlacement += teamsInThisElimination.Count;
                }
            }

            var unplacedTeams = participatingTeams.Where(t => !placedTeamIds.Contains(t.Id)).ToList();
            if (unplacedTeams.Any())
            {
                int lastPlacement = placements.Any() ? placements.Max(p => p.Placement) + 1 : 1;
                foreach (var team in unplacedTeams)
                {
                    if (!placedTeamIds.Contains(team.Id))
                    {
                        placements.Add(CreatePlacement(tournament, team, lastPlacement));
                        placedTeamIds.Add(team.Id);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
