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
            var seriesList = new List<Balkana.Data.Models.Series>();
            int teamCount = teams.Count;

            Console.WriteLine($"🎯 Generating Double Elimination bracket for {teamCount} teams");

            // Calculate the optimal bracket structure
            var bracketConfig = CalculateBracketConfiguration(teamCount);
            
            Console.WriteLine($"🎯 Bracket config: Size={bracketConfig.BracketSize}, FirstRoundTeams={bracketConfig.FirstRoundTeams}, ByeTeams={bracketConfig.ByeTeams}");

            // Generate proper seeding positions
            var seedPositions = GenerateTournamentSeeding(bracketConfig.BracketSize);
            var bracketSlots = new Team[bracketConfig.BracketSize];

            // Place teams in their seeded positions
            for (int i = 0; i < bracketConfig.BracketSize; i++)
            {
                int seedAtPosition = seedPositions[i];
                if (seedAtPosition <= teamCount)
                {
                    bracketSlots[i] = teams[seedAtPosition - 1]; // Convert seed to 0-based index
                    Console.WriteLine($"🎯 Position {i + 1}: Seed {seedAtPosition} = Team {bracketSlots[i].FullName}");
                }
                else
                {
                    bracketSlots[i] = null; // Bye slot
                    Console.WriteLine($"🎯 Position {i + 1}: Bye (seed {seedAtPosition} > {teamCount})");
                }
            }

            // Generate Upper Bracket
            var upperBracketSeries = GenerateUpperBracket(bracketSlots, bracketConfig, tournamentId, shortName, date);
            seriesList.AddRange(upperBracketSeries);

            // Generate Lower Bracket (empty initially - teams will be seeded when they lose)
            var lowerBracketSeries = GenerateLowerBracket(bracketConfig, tournamentId, shortName, date);
            seriesList.AddRange(lowerBracketSeries);

            // Generate Grand Final
            var grandFinal = NewSeries($"{shortName} - Grand Final", tournamentId, null, null, 1, 1, BracketType.GrandFinal, date);
            seriesList.Add(grandFinal);

            // Wire up all progression
            WireUpDoubleEliminationProgression(upperBracketSeries, lowerBracketSeries, grandFinal);

            Console.WriteLine($"🎯 Generated {seriesList.Count} series total");
            return seriesList;
        }

        private BracketConfiguration CalculateBracketConfiguration(int teamCount)
        {
            // For double elimination, we use the same logic as single elimination
            int bracketSize = FindOptimalBracketSize(teamCount);
            
            // Calculate how many teams get byes (skip first round)
            int byeTeams = bracketSize - teamCount;
            int firstRoundTeams = teamCount - byeTeams;

            return new BracketConfiguration
            {
                BracketSize = bracketSize,
                FirstRoundTeams = firstRoundTeams,
                ByeTeams = byeTeams,
                TotalTeams = teamCount
            };
        }

        private int FindOptimalBracketSize(int teamCount)
        {
            // Find the smallest power of 2 that can accommodate all teams
            if (teamCount <= 2) return 2;
            if (teamCount <= 4) return 4;
            if (teamCount <= 8) return 8;
            if (teamCount <= 16) return 16;
            if (teamCount <= 32) return 32;
            if (teamCount <= 64) return 64;
            
            // For larger brackets, find the next power of 2
            int bracketSize = 1;
            while (bracketSize < teamCount)
            {
                bracketSize *= 2;
            }
            return bracketSize;
        }

        private List<Balkana.Data.Models.Series> GenerateUpperBracket(Team[] bracketSlots, BracketConfiguration config, int tournamentId, string shortName, DateTime date)
        {
            var seriesList = new List<Balkana.Data.Models.Series>();
            int totalRounds = (int)Math.Log2(config.BracketSize);

            // Generate all upper bracket rounds
            for (int round = 1; round <= totalRounds; round++)
            {
                int matchesInRound = config.BracketSize / (int)Math.Pow(2, round);
                
                for (int match = 1; match <= matchesInRound; match++)
                {
                    seriesList.Add(NewSeries(
                        $"{shortName} - UB Round {round} Match {match}",
                        tournamentId,
                        null, // Will be filled based on seeding
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

            for (int i = 0; i < config.BracketSize; i += 2)
            {
                var teamA = bracketSlots[i];
                var teamB = bracketSlots[i + 1];

                if (firstRoundMatchIndex < firstRound.Count)
                {
                    if (teamA != null && teamB != null)
                    {
                        // Both teams present - create match
                        firstRound[firstRoundMatchIndex].TeamAId = teamA.Id;
                        firstRound[firstRoundMatchIndex].TeamBId = teamB.Id;
                        Console.WriteLine($"🎯 UB Round 1 Match {firstRoundMatchIndex + 1}: {teamA.FullName} vs {teamB.FullName}");
                    }
                    else if (teamA != null || teamB != null)
                    {
                        // One team present - they get a bye and advance
                        var advancingTeam = teamA ?? teamB;
                        teamsToAdvance.Add(advancingTeam);
                        
                        // Create a bye match for tracking
                        firstRound[firstRoundMatchIndex].TeamAId = teamA?.Id;
                        firstRound[firstRoundMatchIndex].TeamBId = teamB?.Id;
                        Console.WriteLine($"🎯 UB Round 1 Match {firstRoundMatchIndex + 1}: {advancingTeam.FullName} gets bye");
                    }
                    else
                    {
                        // Both null - empty match
                        Console.WriteLine($"🎯 UB Round 1 Match {firstRoundMatchIndex + 1}: Empty match");
                    }
                    firstRoundMatchIndex++;
                }
            }

            // Advance teams with byes to the second round
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
                            Console.WriteLine($"🎯 {team.FullName} advances to UB Round 2 Match {secondRoundIndex + 1} (TeamA)");
                        }
                        else if (secondRound[secondRoundIndex].TeamBId == null)
                        {
                            secondRound[secondRoundIndex].TeamBId = team.Id;
                            Console.WriteLine($"🎯 {team.FullName} advances to UB Round 2 Match {secondRoundIndex + 1} (TeamB)");
                        }
                        secondRoundIndex++;
                    }
                }
            }

            return seriesList;
        }

        private List<Balkana.Data.Models.Series> GenerateLowerBracket(BracketConfiguration config, int tournamentId, string shortName, DateTime date)
        {
            var seriesList = new List<Balkana.Data.Models.Series>();
            
            // Calculate number of rounds needed for lower bracket
            // Standard double elimination: LB has (2 * log2(bracketSize) - 2) rounds
            int totalRounds = (int)Math.Log2(config.BracketSize) * 2 - 2;
            
            Console.WriteLine($"🎯 Generating Lower Bracket with {totalRounds} rounds for {config.BracketSize}-team bracket");
            
            for (int round = 1; round <= totalRounds; round++)
            {
                int matchesInRound = CalculateLowerBracketMatchesInRound(round, config.BracketSize);
                
                Console.WriteLine($"🎯 LB Round {round}: {matchesInRound} matches");

                for (int match = 1; match <= matchesInRound; match++)
                {
                    // Create lower bracket series with descriptive names showing where teams come from
                    string seriesName = $"{shortName} - LB Round {round} Match {match}";
                    
                    // Add context about where teams come from
                    if (round == 1)
                    {
                        seriesName += " [Losers from UB Round 1]";
                    }
                    else if (round == 2)
                    {
                        seriesName += " [LB R1 Winners vs UB R2 Losers]";
                    }
                    else if (round == 3)
                    {
                        seriesName += " [LB R2 Winners]";
                    }
                    else if (round == 4)
                    {
                        seriesName += " [LB R3 Winner vs UB Final Loser]";
                    }
                    
                    seriesList.Add(NewSeries(
                        seriesName,
                        tournamentId,
                        null, // Lower bracket starts empty - teams seeded when they lose
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


        private int CalculateLowerBracketMatchesInRound(int round, int bracketSize)
        {
            // Calculate matches in lower bracket round based on standard double elimination structure
            if (round == 1)
            {
                // First round: half the teams that lost in upper bracket first round
                return bracketSize / 4;
            }
            else if (round == 2)
            {
                // Second round: same as first round
                return bracketSize / 4;
            }
            else if (round == 3)
            {
                // Third round: 1 match (winners from LB R2)
                return 1;
            }
            else if (round == 4)
            {
                // Fourth round: 1 match (winner from LB R3 vs loser from UB Final)
                return 1;
            }
            else
            {
                // Fallback for other bracket sizes
                return Math.Max(1, bracketSize / (int)Math.Pow(2, round + 1));
            }
        }

        private int[] GenerateTournamentSeeding(int bracketSize)
        {
            // Generate proper tournament seeding using the standard algorithm
            // This creates the correct bracket positions for proper tournament seeding
            
            if (bracketSize == 2) return new[] { 1, 2 };
            if (bracketSize == 4) return new[] { 1, 4, 2, 3 };
            if (bracketSize == 8) return new[] { 1, 8, 4, 5, 2, 7, 3, 6 };
            if (bracketSize == 16) return new[] { 1, 16, 8, 9, 4, 13, 5, 12, 2, 15, 7, 10, 3, 14, 6, 11 };
            if (bracketSize == 32) return new[] { 1, 32, 16, 17, 8, 25, 9, 24, 4, 29, 13, 20, 5, 28, 12, 21, 2, 31, 15, 18, 7, 26, 10, 23, 3, 30, 14, 19, 6, 27, 11, 22 };
            
            // For larger brackets, use the standard tournament seeding algorithm
            var result = new int[bracketSize];
            
            // Initialize with seed 1 at position 1
            result[0] = 1;
            
            // Use the standard tournament seeding pattern
            for (int i = 1; i < bracketSize; i++)
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

        public void WireUpDoubleEliminationProgression(List<Balkana.Data.Models.Series> upperBracketSeries, List<Balkana.Data.Models.Series> lowerBracketSeries, Balkana.Data.Models.Series grandFinal)
        {
            // Wire up Upper Bracket progression
            WireUpUpperBracketProgression(upperBracketSeries);

            // Wire up Lower Bracket progression
            WireUpLowerBracketProgression(lowerBracketSeries);

            // Wire up Upper Bracket losers to Lower Bracket
            WireUpUpperBracketLosersToLowerBracket(upperBracketSeries, lowerBracketSeries);

            // Wire up finals to Grand Final
            WireUpFinalsToGrandFinal(upperBracketSeries, lowerBracketSeries, grandFinal);
        }

        private void WireUpUpperBracketProgression(List<Balkana.Data.Models.Series> upperBracketSeries)
        {
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
                        Console.WriteLine($"🎯 Wired UB Round {roundIndex + 1} Match {i + 1} & {i + 2} -> UB Round {roundIndex + 2} Match {i / 2 + 1}");
                    }
                }
            }
        }

        private void WireUpLowerBracketProgression(List<Balkana.Data.Models.Series> lowerBracketSeries)
        {
            var lowerBracketByRound = lowerBracketSeries.GroupBy(s => s.Round).OrderBy(g => g.Key).ToList();

            for (int roundIndex = 0; roundIndex < lowerBracketByRound.Count - 1; roundIndex++)
            {
                var currentRound = lowerBracketByRound[roundIndex].OrderBy(s => s.Position).ToList();
                var nextRound = lowerBracketByRound[roundIndex + 1].OrderBy(s => s.Position).ToList();

                if (roundIndex == 0) // LB Round 1 to LB Round 2
                {
                    // For 7-team bracket: LB 1.1 goes to LB 2.1, LB 1.2 goes to LB 2.2
                    if (currentRound.Count == 2 && nextRound.Count == 2)
                    {
                        // LB 1.1 (VAGMASTERS vs Bulletproof) -> LB 2.1
                        currentRound[0].NextSeriesId = nextRound[0].Id;
                        Console.WriteLine($"🎯 Wired LB Round 1 Match 1 -> LB Round 2 Match 1");
                        
                        // LB 1.2 (Banana B vs TBD) -> LB 2.2
                        currentRound[1].NextSeriesId = nextRound[1].Id;
                        Console.WriteLine($"🎯 Wired LB Round 1 Match 2 -> LB Round 2 Match 2");
                    }
                    else
                    {
                        // Fallback to original logic for other bracket sizes
                        for (int i = 0; i < currentRound.Count; i += 2)
                        {
                            if (i + 1 < currentRound.Count && i / 2 < nextRound.Count)
                            {
                                currentRound[i].NextSeriesId = nextRound[i / 2].Id;
                                currentRound[i + 1].NextSeriesId = nextRound[i / 2].Id;
                                Console.WriteLine($"🎯 Wired LB Round {roundIndex + 1} Match {i + 1} & {i + 2} -> LB Round {roundIndex + 2} Match {i / 2 + 1}");
                            }
                        }
                    }
                }
                else
                {
                    // For other rounds, handle both single matches and paired matches
                    if (currentRound.Count == 1 && nextRound.Count == 1)
                    {
                        // Single match to single match (e.g., LB Round 3 to LB Round 4)
                        currentRound[0].NextSeriesId = nextRound[0].Id;
                        Console.WriteLine($"🎯 Wired LB Round {roundIndex + 1} Match 1 -> LB Round {roundIndex + 2} Match 1");
                    }
                    else
                    {
                        // Original logic for paired matches (2 matches feed into 1)
                        for (int i = 0; i < currentRound.Count; i += 2)
                        {
                            if (i + 1 < currentRound.Count && i / 2 < nextRound.Count)
                            {
                                currentRound[i].NextSeriesId = nextRound[i / 2].Id;
                                currentRound[i + 1].NextSeriesId = nextRound[i / 2].Id;
                                Console.WriteLine($"🎯 Wired LB Round {roundIndex + 1} Match {i + 1} & {i + 2} -> LB Round {roundIndex + 2} Match {i / 2 + 1}");
                            }
                        }
                    }
                }
            }
        }

        private void WireUpUpperBracketLosersToLowerBracket(List<Balkana.Data.Models.Series> upperBracketSeries, List<Balkana.Data.Models.Series> lowerBracketSeries)
        {
            // In double elimination, we need to establish the relationship between upper bracket losers and lower bracket
            // The lower bracket starts empty - teams will be seeded when they actually lose matches
            // This method just establishes the structure and relationships
            
            var upperBracketByRound = upperBracketSeries.GroupBy(s => s.Round).OrderBy(g => g.Key).ToList();
            var lowerBracketByRound = lowerBracketSeries.GroupBy(s => s.Round).OrderBy(g => g.Key).ToList();

            Console.WriteLine("🎯 Establishing Upper Bracket to Lower Bracket relationships");

            // For each upper bracket round (except the final), losers drop to lower bracket
            for (int roundIndex = 0; roundIndex < upperBracketByRound.Count - 1; roundIndex++)
            {
                var upperRound = upperBracketByRound[roundIndex].OrderBy(s => s.Position).ToList();
                
                Console.WriteLine($"🎯 Processing UB Round {roundIndex + 1} with {upperRound.Count} matches");
                
                // Find corresponding lower bracket round
                // In double elimination, upper bracket round N losers drop to lower bracket round N
                var correspondingLowerRound = lowerBracketByRound.FirstOrDefault(g => g.Key == roundIndex + 1);
                if (correspondingLowerRound != null)
                {
                    var lowerRound = correspondingLowerRound.OrderBy(s => s.Position).ToList();
                    
                    Console.WriteLine($"🎯 Found corresponding LB Round {roundIndex + 1} with {lowerRound.Count} matches");
                    
                    // Establish the relationship without seeding teams
                    for (int i = 0; i < upperRound.Count && i < lowerRound.Count; i++)
                    {
                        var upperSeries = upperRound[i];
                        var lowerSeries = lowerRound[i];
                        
                        Console.WriteLine($"🎯 UB Round {roundIndex + 1} Match {i + 1} loser will drop to LB Round {roundIndex + 1} Match {i + 1}");
                        
                        // Just log the relationship - don't seed any teams
                        // Teams will be seeded into lower bracket when they actually lose matches
                        if (roundIndex == 0 && upperSeries.TeamAId != null && upperSeries.TeamBId != null)
                        {
                            Console.WriteLine($"🎯 LB Round 1 Match {i + 1} will receive the loser of UB Round 1 Match {i + 1} (Team {upperSeries.TeamAId} vs Team {upperSeries.TeamBId})");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"🎯 No corresponding LB Round {roundIndex + 1} found for UB Round {roundIndex + 1}");
                }
            }
            
            Console.WriteLine("🎯 Lower bracket structure established - teams will be seeded when they lose matches");
        }

        private void WireUpFinalsToGrandFinal(List<Balkana.Data.Models.Series> upperBracketSeries, List<Balkana.Data.Models.Series> lowerBracketSeries, Balkana.Data.Models.Series grandFinal)
        {
            // Wire up Upper Bracket Final to Grand Final
            var upperBracketFinal = upperBracketSeries.OrderByDescending(s => s.Round).FirstOrDefault();
            if (upperBracketFinal != null)
            {
                upperBracketFinal.NextSeriesId = grandFinal.Id;
                Console.WriteLine($"🎯 Wired UB Final -> Grand Final");
            }

            // Wire up Lower Bracket Final to Grand Final
            var lowerBracketFinal = lowerBracketSeries.OrderByDescending(s => s.Round).FirstOrDefault();
            if (lowerBracketFinal != null)
            {
                lowerBracketFinal.NextSeriesId = grandFinal.Id;
                Console.WriteLine($"🎯 Wired LB Final -> Grand Final");
            }
        }
    }
}
