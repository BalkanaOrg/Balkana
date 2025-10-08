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

        public List<Balkana.Data.Models.Series> GenerateBracket(int tournamentId, List<Team> teams)
        {
            var tournament = data.Tournaments
                .Include(t => t.TournamentTeams).ThenInclude(tt => tt.Team)
                .FirstOrDefault(t => t.Id == tournamentId);

            if (tournament == null)
                throw new Exception("Tournament not found");

            if (tournament.Elimination == EliminationType.Single)
                return GenerateSingleElimination(teams, tournamentId, tournament.ShortName ?? $"Tournament {tournament.Id}", tournament.StartDate);
            else
                return new DoubleEliminationBracketService(data, mapper)
                    .GenerateDoubleElimination(teams, tournamentId, tournament.ShortName ?? $"Tournament {tournament.Id}", tournament.StartDate);
        }

        private List<Balkana.Data.Models.Series> GenerateSingleElimination(List<Team> teams, int tournamentId, string shortName, DateTime date)
        {
            var seriesList = new List<Balkana.Data.Models.Series>();

            int teamCount = teams.Count;
            
            // Calculate optimal bracket size and byes
            int bracketSize = CalculateOptimalBracketSize(teamCount);
            int byes = bracketSize - teamCount;

            // Generate proper seeding positions (1 vs 4, 2 vs 3, etc.)
            var seedPositions = GenerateSeedPositions(bracketSize);
            var slots = new Team[bracketSize];

            // Place teams in their seeded positions
            // seedPositions[i] tells us which seed should be at position i
            // teams[i] is the team with seed (i+1)
            for (int i = 0; i < bracketSize; i++)
            {
                int seedAtThisPosition = seedPositions[i];
                if (seedAtThisPosition <= teamCount)
                {
                    // Place the team with this seed at this position
                    slots[i] = teams[seedAtThisPosition - 1]; // Convert seed to 0-based index
                }
                else
                {
                    // This position gets a bye (no team)
                    slots[i] = null;
                }
            }

            // Generate complete bracket structure
            int totalRounds = (int)Math.Log2(bracketSize);
            var allSeries = new List<Balkana.Data.Models.Series>[totalRounds];

            // Initialize each round
            for (int round = 0; round < totalRounds; round++)
            {
                int matchesInRound = bracketSize / (int)Math.Pow(2, round + 1);
                allSeries[round] = new List<Balkana.Data.Models.Series>();
                
                for (int match = 1; match <= matchesInRound; match++)
                {
                    allSeries[round].Add(NewSeries(
                        $"{shortName} - Round {round + 1} Match {match}",
                        tournamentId,
                        null, // Will be filled based on seeding
                        null,
                        round + 1,
                        match,
                        BracketType.Upper,
                        date
                    ));
                }
            }

            // Fill in the first round with actual teams, handling byes properly
            var firstRound = allSeries[0];
            int firstRoundMatchIndex = 0;
            var teamsToAdvance = new List<Team>(); // Teams that get byes

            for (int i = 0; i < bracketSize; i += 2)
            {
                var teamA = slots[i];
                var teamB = slots[i + 1];

                if (firstRoundMatchIndex < firstRound.Count)
                {
                    if (teamA != null && teamB != null)
                    {
                        // Both teams present - create match
                        firstRound[firstRoundMatchIndex].TeamAId = teamA.Id;
                        firstRound[firstRoundMatchIndex].TeamBId = teamB.Id;
                        firstRoundMatchIndex++;
                    }
                    else if (teamA != null || teamB != null)
                    {
                        // One team present - they get a bye and advance
                        var advancingTeam = teamA ?? teamB;
                        teamsToAdvance.Add(advancingTeam);
                        
                        // Create a bye match for tracking
                        firstRound[firstRoundMatchIndex].TeamAId = teamA?.Id;
                        firstRound[firstRoundMatchIndex].TeamBId = teamB?.Id;
                        firstRoundMatchIndex++;
                    }
                    else
                    {
                        // Both null - empty match
                        firstRoundMatchIndex++;
                    }
                }
            }

            // Advance teams with byes to the next round
            if (teamsToAdvance.Any() && allSeries.Length > 1)
            {
                var secondRound = allSeries[1];
                int secondRoundIndex = 0;
                
                foreach (var team in teamsToAdvance)
                {
                    if (secondRoundIndex < secondRound.Count)
                    {
                        // Place the team in the second round
                        if (secondRound[secondRoundIndex].TeamAId == null)
                        {
                            secondRound[secondRoundIndex].TeamAId = team.Id;
                        }
                        else if (secondRound[secondRoundIndex].TeamBId == null)
                        {
                            secondRound[secondRoundIndex].TeamBId = team.Id;
                        }
                        secondRoundIndex++;
                    }
                }
            }

            // Add all series to the result
            foreach (var round in allSeries)
            {
                seriesList.AddRange(round);
            }

            return seriesList;
        }


        private int[] GenerateSeedPositions(int bracketSize)
        {
            if (bracketSize == 1) return new[] { 1 };
            if (bracketSize == 2) return new[] { 1, 2 };
            if (bracketSize == 4) return new[] { 1, 4, 2, 3 };
            if (bracketSize == 8) return new[] { 1, 8, 4, 5, 2, 7, 3, 6 };
            if (bracketSize == 16) return new[] { 1, 16, 8, 9, 4, 13, 5, 12, 2, 15, 7, 10, 3, 14, 6, 11 };

            // For larger brackets, use proper tournament seeding algorithm
            return GenerateTournamentSeeding(bracketSize);
        }

        private int[] GenerateTournamentSeeding(int bracketSize)
        {
            var result = new int[bracketSize];
            
            // Use the standard tournament seeding algorithm
            // For each position, calculate which seed should be there
            for (int i = 0; i < bracketSize; i++)
            {
                result[i] = CalculateSeedForPosition(i + 1, bracketSize);
            }
            
            return result;
        }

        private int CalculateSeedForPosition(int position, int bracketSize)
        {
            if (position == 1) return 1;
            if (position == bracketSize) return 2;
            
            // For positions in between, use the standard tournament seeding formula
            int seed = 1;
            int temp = position - 1;
            
            while (temp > 0)
            {
                if (temp % 2 == 1)
                {
                    seed = bracketSize + 1 - seed;
                }
                temp /= 2;
            }
            
            return seed;
        }

        private Balkana.Data.Models.Series NewSeries(string name, int tournamentId, Team teamA, Team teamB, int round, int pos, BracketType bracket, DateTime date)
        {
            return new Balkana.Data.Models.Series
            {
                Name = name,
                TournamentId = tournamentId,
                TeamAId = teamA?.Id,
                TeamBId = teamB?.Id,
                Round = round,
                Position = pos,
                Bracket = bracket,
                DatePlayed = date
            };
        }


        private int CalculateOptimalBracketSize(int teamCount)
        {
            // Round DOWN to previous power-of-2 bracket size
            // This ensures we use the largest bracket that is <= teamCount
            if (teamCount <= 2) return 2;
            if (teamCount <= 4) return 4;
            if (teamCount <= 8) return 8;
            if (teamCount <= 16) return 16;
            if (teamCount <= 32) return 32;
            if (teamCount <= 64) return 64;
            
            // For larger brackets, find the largest power of 2 that is <= teamCount
            int bracketSize = 1;
            while (bracketSize * 2 <= teamCount)
            {
                bracketSize *= 2;
            }
            return bracketSize;
        }

        public void WireUpSeriesProgression(List<Balkana.Data.Models.Series> seriesList)
        {
            // Group series by round
            var seriesByRound = seriesList.GroupBy(s => s.Round).OrderBy(g => g.Key).ToList();

            for (int roundIndex = 0; roundIndex < seriesByRound.Count - 1; roundIndex++)
            {
                var currentRound = seriesByRound[roundIndex].OrderBy(s => s.Position).ToList();
                var nextRound = seriesByRound[roundIndex + 1].OrderBy(s => s.Position).ToList();

                // Wire up progression: each pair of matches in current round feeds into one match in next round
                for (int i = 0; i < currentRound.Count; i += 2)
                {
                    if (i + 1 < currentRound.Count && i / 2 < nextRound.Count)
                    {
                        currentRound[i].NextSeriesId = nextRound[i / 2].Id;
                        currentRound[i + 1].NextSeriesId = nextRound[i / 2].Id;
                    }
                }
            }
        }

        private int NextPowerOfTwo(int n)
        {
            if (n < 1) return 1;
            int p = 1;
            while (p < n) p <<= 1;
            return p;
        }
    }
}