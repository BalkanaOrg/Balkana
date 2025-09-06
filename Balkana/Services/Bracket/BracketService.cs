using AutoMapper;
using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Bracket
{
    public class BracketService
    {
        private readonly ApplicationDbContext data;
        private readonly IMapper mapper;

        public BracketService(ApplicationDbContext data, IMapper mapper)
        {
            this.data = data;
            this.mapper = mapper;
        }

        public List<Balkana.Data.Models.Series> GenerateBracket(int tournamentId)
        {
            var tournament = data.Tournaments
                .Include(t => t.TournamentTeams).ThenInclude(tt => tt.Team)
                .FirstOrDefault(t => t.Id == tournamentId);

            if (tournament == null)
                throw new Exception("Tournament not found");

            var teams = tournament.TournamentTeams
                .OrderBy(tt => tt.Seed)
                .Select(tt => tt.Team)
                .ToList();

            if (tournament.Elimination == EliminationType.Single)
                return GenerateSingleElimination(teams, tournamentId, tournament.ShortName ?? $"Tournament {tournament.Id}");
            else
                return new DoubleEliminationBracketService(data, mapper)
                    .GenerateDoubleElimination(teams, tournamentId);
        }

        private List<Balkana.Data.Models.Series> GenerateSingleElimination(List<Team> teams, int tournamentId, string shortName)
        {
            var seriesList = new List<Balkana.Data.Models.Series>();

            int teamCount = teams.Count;
            int bracketSize = NextPowerOfTwo(teamCount); // e.g. 6 → 8, 10 → 16
            int byes = bracketSize - teamCount;

            // --- Rule: top seeds get byes directly into Round 2 ---
            var autoQualified = teams.Take(byes).ToList(); // first N seeds get byes
            var queue = new Queue<Team>(teams.Skip(byes)); // rest must play Round 1

            int round = 1;
            int matchNumber = 1;

            // --- ROUND 1 (non-bye teams play) ---
            int matchesInRound1 = (teamCount - byes) / 2;
            for (int i = 0; i < matchesInRound1; i++)
            {
                var teamA = queue.Dequeue();
                var teamB = queue.Dequeue();

                var match = NewSeries(
                    $"{shortName} - Round {round} Match {matchNumber}",
                    tournamentId,
                    teamA,
                    teamB,
                    round,
                    matchNumber,
                    BracketType.Upper
                );
                seriesList.Add(match);
                matchNumber++;
            }

            // --- SUBSEQUENT ROUNDS ---
            int totalRounds = (int)Math.Log2(bracketSize);
            for (int r = 2; r <= totalRounds; r++)
            {
                int matchesInRound = bracketSize / (int)Math.Pow(2, r);

                for (int i = 0; i < matchesInRound; i++)
                {
                    Team teamA = null;
                    Team teamB = null;

                    // Correct placement: distribute autoQualified seeds across different matches
                    if (r == 2 && autoQualified.Count > 0)
                    {
                        // First bye goes into Semifinal 1, second into Semifinal 2, etc.
                        var seed = autoQualified[0];
                        autoQualified.RemoveAt(0);

                        if (i % 2 == 0) teamA = seed; // odd semifinal → left slot
                        else teamB = seed;            // even semifinal → right slot
                    }

                    var match = NewSeries(
                        $"{shortName} - Round {r} Match {i + 1}",
                        tournamentId,
                        teamA,
                        teamB,
                        r,
                        i + 1,
                        BracketType.Upper
                    );
                    seriesList.Add(match);
                }
            }

            return seriesList;
        }

        private Balkana.Data.Models.Series NewSeries(string name, int tournamentId, Team teamA, Team teamB, int round, int pos, BracketType bracket)
        {
            return new Balkana.Data.Models.Series
            {
                Name = name,
                TournamentId = tournamentId,
                TeamAId = teamA?.Id,
                TeamBId = teamB?.Id,
                Round = round,
                Position = pos,
                Bracket = bracket
            };
        }

        private int NextPowerOfTwo(int n)
        {
            int p = 1;
            while (p < n) p *= 2;
            return p;
        }
    }
}