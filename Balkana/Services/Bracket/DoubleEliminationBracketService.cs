using AutoMapper;
using Balkana.Data;
using Balkana.Data.Models;

namespace Balkana.Services.Bracket
{
    public class DoubleEliminationBracketService
    {
        private readonly ApplicationDbContext data;
        private readonly IMapper mapper;

        public DoubleEliminationBracketService(ApplicationDbContext data, IMapper mapper)
        {
            this.mapper = mapper;
            this.data = data;
        }

        public List<Balkana.Data.Models.Series> GenerateDoubleElimination(List<Team> teams, int tournamentId, string shortName, DateTime date)
        {
            int teamCount = teams.Count;
            int bracketSize = CalculateOptimalBracketSize(teamCount);
            var seriesList = new List<Balkana.Data.Models.Series>();

            // Generate proper seeding positions
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

            // Generate complete Upper Bracket structure
            var upperBracketSeries = GenerateCompleteUpperBracket(slots, tournamentId, shortName, date);
            seriesList.AddRange(upperBracketSeries);

            // Generate complete Lower Bracket structure
            var lowerBracketSeries = GenerateCompleteLowerBracket(bracketSize, tournamentId, shortName, date);
            seriesList.AddRange(lowerBracketSeries);

            // Generate Grand Final
            var grandFinal = NewSeries($"{shortName} - Grand Final", tournamentId, null, null, 1, 1, BracketType.GrandFinal, date);
            seriesList.Add(grandFinal);

            return seriesList;
        }

        private List<Balkana.Data.Models.Series> GenerateCompleteUpperBracket(Team[] slots, int tournamentId, string shortName, DateTime date)
        {
            var seriesList = new List<Balkana.Data.Models.Series>();
            int bracketSize = slots.Length;
            int totalRounds = (int)Math.Log2(bracketSize);

            // Generate all upper bracket rounds
            for (int round = 1; round <= totalRounds; round++)
            {
                int matchesInRound = bracketSize / (int)Math.Pow(2, round);
                
                for (int match = 1; match <= matchesInRound; match++)
                {
                    seriesList.Add(NewSeries(
                        $"{shortName} - UB Round {round} Match {match}",
                        tournamentId,
                        null, // Will be filled based on seeding for first round
                        null,
                        round,
                        match,
                        BracketType.Upper,
                        date
                    ));
                }
            }

            // Fill in the first round with actual teams, handling byes properly
            var firstRound = seriesList.Where(s => s.Round == 1 && s.Bracket == BracketType.Upper).ToList();
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
            if (teamsToAdvance.Any())
            {
                var secondRound = seriesList.Where(s => s.Round == 2 && s.Bracket == BracketType.Upper).ToList();
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

            return seriesList;
        }

        private List<Balkana.Data.Models.Series> GenerateCompleteLowerBracket(int bracketSize, int tournamentId, string shortName, DateTime date)
        {
            var seriesList = new List<Balkana.Data.Models.Series>();
            
            // Calculate number of rounds needed for lower bracket
            // Lower bracket has (2 * log2(bracketSize) - 1) rounds
            int totalRounds = (int)Math.Log2(bracketSize) * 2 - 1;
            
            for (int round = 1; round <= totalRounds; round++)
            {
                int matchesInRound;
                
                if (round == 1)
                {
                    // First round: losers from upper bracket first round
                    matchesInRound = bracketSize / 2;
                }
                else if (round <= (int)Math.Log2(bracketSize))
                {
                    // Early rounds: half the matches of previous round
                    matchesInRound = bracketSize / (int)Math.Pow(2, round);
                }
                else
                {
                    // Later rounds: alternating pattern
                    int upperBracketRounds = (int)Math.Log2(bracketSize);
                    int lowerBracketRound = round - upperBracketRounds;
                    matchesInRound = bracketSize / (int)Math.Pow(2, upperBracketRounds - lowerBracketRound + 1);
                }

                for (int match = 1; match <= matchesInRound; match++)
                {
                    seriesList.Add(NewSeries(
                        $"{shortName} - LB Round {round} Match {match}",
                        tournamentId,
                        null,
                        null,
                        round,
                        match,
                        BracketType.Lower,
                        date
                    ));
                }
            }

            return seriesList;
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
            // Round down to previous power-of-2 bracket size
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

        private int NextPowerOfTwo(int n)
        {
            int p = 1;
            while (p < n) p *= 2;
            return p;
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

        public void WireUpDoubleEliminationProgression(List<Balkana.Data.Models.Series> seriesList)
        {
            // Wire up Upper Bracket progression
            var upperBracketSeries = seriesList.Where(s => s.Bracket == BracketType.Upper).OrderBy(s => s.Round).ThenBy(s => s.Position).ToList();
            var upperBracketByRound = upperBracketSeries.GroupBy(s => s.Round).OrderBy(g => g.Key).ToList();

            for (int roundIndex = 0; roundIndex < upperBracketByRound.Count - 1; roundIndex++)
            {
                var currentRound = upperBracketByRound[roundIndex].OrderBy(s => s.Position).ToList();
                var nextRound = upperBracketByRound[roundIndex + 1].OrderBy(s => s.Position).ToList();

                for (int i = 0; i < currentRound.Count; i += 2)
                {
                    if (i + 1 < currentRound.Count && i / 2 < nextRound.Count)
                    {
                        currentRound[i].NextSeriesId = nextRound[i / 2].Id;
                        currentRound[i + 1].NextSeriesId = nextRound[i / 2].Id;
                    }
                }
            }

            // Wire up Lower Bracket progression
            var lowerBracketSeries = seriesList.Where(s => s.Bracket == BracketType.Lower).OrderBy(s => s.Round).ThenBy(s => s.Position).ToList();
            var lowerBracketByRound = lowerBracketSeries.GroupBy(s => s.Round).OrderBy(g => g.Key).ToList();

            for (int roundIndex = 0; roundIndex < lowerBracketByRound.Count - 1; roundIndex++)
            {
                var currentRound = lowerBracketByRound[roundIndex].OrderBy(s => s.Position).ToList();
                var nextRound = lowerBracketByRound[roundIndex + 1].OrderBy(s => s.Position).ToList();

                for (int i = 0; i < currentRound.Count; i += 2)
                {
                    if (i + 1 < currentRound.Count && i / 2 < nextRound.Count)
                    {
                        currentRound[i].NextSeriesId = nextRound[i / 2].Id;
                        currentRound[i + 1].NextSeriesId = nextRound[i / 2].Id;
                    }
                }
            }

            // Wire up Upper Bracket losers to Lower Bracket
            WireUpUpperBracketLosersToLowerBracket(upperBracketSeries, lowerBracketSeries);

            // Wire up Upper Bracket Final to Grand Final
            var upperBracketFinal = upperBracketSeries.Where(s => s.Round == upperBracketByRound.Last().Key).FirstOrDefault();
            var grandFinal = seriesList.Where(s => s.Bracket == BracketType.GrandFinal).FirstOrDefault();
            if (upperBracketFinal != null && grandFinal != null)
            {
                upperBracketFinal.NextSeriesId = grandFinal.Id;
            }

            // Wire up Lower Bracket Final to Grand Final
            var lowerBracketFinal = lowerBracketSeries.Where(s => s.Round == lowerBracketByRound.Last().Key).FirstOrDefault();
            if (lowerBracketFinal != null && grandFinal != null)
            {
                lowerBracketFinal.NextSeriesId = grandFinal.Id;
            }
        }

        private void WireUpUpperBracketLosersToLowerBracket(List<Balkana.Data.Models.Series> upperBracketSeries, List<Balkana.Data.Models.Series> lowerBracketSeries)
        {
            // In double elimination, upper bracket losers drop to specific lower bracket positions
            // This is a simplified approach - in a real implementation, you'd need more sophisticated logic
            
            var upperBracketByRound = upperBracketSeries.GroupBy(s => s.Round).OrderBy(g => g.Key).ToList();
            var lowerBracketByRound = lowerBracketSeries.GroupBy(s => s.Round).OrderBy(g => g.Key).ToList();

            // For each upper bracket round (except the final), losers drop to lower bracket
            for (int roundIndex = 0; roundIndex < upperBracketByRound.Count - 1; roundIndex++)
            {
                var upperRound = upperBracketByRound[roundIndex].OrderBy(s => s.Position).ToList();
                
                // Find corresponding lower bracket round
                // In double elimination, upper bracket round N losers drop to lower bracket round N
                var correspondingLowerRound = lowerBracketByRound.FirstOrDefault(g => g.Key == roundIndex + 1);
                if (correspondingLowerRound != null)
                {
                    var lowerRound = correspondingLowerRound.OrderBy(s => s.Position).ToList();
                    
                    // Wire up upper bracket losers to lower bracket
                    for (int i = 0; i < upperRound.Count && i < lowerRound.Count; i++)
                    {
                        // Each upper bracket series loser drops to the corresponding lower bracket position
                        // Note: This is a simplified approach - real double elimination has more complex mapping
                        upperRound[i].NextSeriesId = lowerRound[i].Id;
                    }
                }
            }
        }
    }
}
