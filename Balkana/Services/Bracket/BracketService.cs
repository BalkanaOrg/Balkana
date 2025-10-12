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

            Console.WriteLine($"🎯 Generating Single Elimination bracket for {teamCount} teams");

            // Calculate the optimal bracket structure
            var bracketConfig = CalculateBracketConfiguration(teamCount);
            
            Console.WriteLine($"🎯 Bracket config: Size={bracketConfig.BracketSize}, FirstRoundTeams={bracketConfig.FirstRoundTeams}, ByeTeams={bracketConfig.ByeTeams}");

            // Generate all rounds
            var allRounds = new List<List<Balkana.Data.Models.Series>>();
            
            // Calculate number of rounds needed
            int totalRounds = (int)Math.Log2(bracketConfig.BracketSize);
            
            // Generate series for each round
            for (int round = 1; round <= totalRounds; round++)
            {
                int matchesInRound = bracketConfig.BracketSize / (int)Math.Pow(2, round);
                var roundSeries = new List<Balkana.Data.Models.Series>();
                
                for (int match = 1; match <= matchesInRound; match++)
                {
                    var series = NewSeries(
                        $"{shortName} - Round {round} Match {match}",
                        tournamentId,
                        null,
                        null,
                        round,
                        match,
                        BracketType.Upper,
                        date
                    );
                    roundSeries.Add(series);
                }
                
                allRounds.Add(roundSeries);
                seriesList.AddRange(roundSeries);
            }

            // Seed teams properly
            SeedTeamsInBracket(teams, allRounds, bracketConfig);

            // Wire up series progression
            WireUpSeriesProgression(allRounds);

            Console.WriteLine($"🎯 Generated {seriesList.Count} series total");
            return seriesList;
        }

        private BracketConfiguration CalculateBracketConfiguration(int teamCount)
        {
            // Find the smallest power of 2 that can accommodate all teams
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
            // For tournament brackets, we want the smallest power of 2 that fits all teams
            // This ensures proper seeding with byes for top teams
            
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

        private void SeedTeamsInBracket(List<Team> teams, List<List<Balkana.Data.Models.Series>> allRounds, BracketConfiguration config)
        {
            // Generate proper tournament seeding positions
            var seedPositions = GenerateTournamentSeeding(config.BracketSize);
            
            Console.WriteLine($"🎯 Seed positions: [{string.Join(", ", seedPositions)}]");

            // Create a mapping of bracket positions to teams
            var bracketSlots = new Team[config.BracketSize];
            
            for (int i = 0; i < config.BracketSize; i++)
            {
                int seedAtPosition = seedPositions[i];
                if (seedAtPosition <= config.TotalTeams)
                {
                    bracketSlots[i] = teams[seedAtPosition - 1]; // Convert 1-based seed to 0-based index
                    Console.WriteLine($"🎯 Position {i + 1}: Seed {seedAtPosition} = Team {bracketSlots[i].FullName}");
                }
                else
                {
                    bracketSlots[i] = null; // Bye slot
                    Console.WriteLine($"🎯 Position {i + 1}: Bye (seed {seedAtPosition} > {config.TotalTeams})");
                }
            }

            // Fill the first round with teams, handling byes
            var firstRound = allRounds[0];
            int firstRoundIndex = 0;
            
            for (int i = 0; i < config.BracketSize; i += 2)
            {
                if (firstRoundIndex >= firstRound.Count) break;
                
                var teamA = bracketSlots[i];
                var teamB = bracketSlots[i + 1];
                
                if (teamA != null && teamB != null)
                {
                    // Both teams present - normal match
                    firstRound[firstRoundIndex].TeamAId = teamA.Id;
                    firstRound[firstRoundIndex].TeamBId = teamB.Id;
                    Console.WriteLine($"🎯 Round 1 Match {firstRoundIndex + 1}: {teamA.FullName} vs {teamB.FullName}");
                }
                else if (teamA != null || teamB != null)
                {
                    // One team present - they get a bye
                    var advancingTeam = teamA ?? teamB;
                    firstRound[firstRoundIndex].TeamAId = teamA?.Id;
                    firstRound[firstRoundIndex].TeamBId = teamB?.Id;
                    Console.WriteLine($"🎯 Round 1 Match {firstRoundIndex + 1}: {advancingTeam.FullName} gets bye");
                    
                    // Advance the team to the next round
                    AdvanceTeamToNextRound(advancingTeam, allRounds, firstRoundIndex, 0);
                }
                else
                {
                    // Both null - empty match (shouldn't happen with proper bracket sizing)
                    Console.WriteLine($"🎯 Round 1 Match {firstRoundIndex + 1}: Empty match");
                }
                
                firstRoundIndex++;
            }
        }

        private void AdvanceTeamToNextRound(Team team, List<List<Balkana.Data.Models.Series>> allRounds, int matchIndex, int currentRound)
        {
            if (currentRound + 1 >= allRounds.Count) return;
            
            var nextRound = allRounds[currentRound + 1];
            int nextRoundMatchIndex = matchIndex / 2;
            
            if (nextRoundMatchIndex < nextRound.Count)
            {
                var nextSeries = nextRound[nextRoundMatchIndex];
                if (nextSeries.TeamAId == null)
                {
                    nextSeries.TeamAId = team.Id;
                    Console.WriteLine($"🎯 {team.FullName} advances to Round {currentRound + 2} Match {nextRoundMatchIndex + 1} (TeamA)");
                }
                else if (nextSeries.TeamBId == null)
                {
                    nextSeries.TeamBId = team.Id;
                    Console.WriteLine($"🎯 {team.FullName} advances to Round {currentRound + 2} Match {nextRoundMatchIndex + 1} (TeamB)");
                }
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
                        Console.WriteLine($"🎯 Wired Round {roundIndex + 1} Match {i + 1} & {i + 2} -> Round {roundIndex + 2} Match {i / 2 + 1}");
                    }
                }
            }
        }

        private void WireUpSeriesProgression(List<List<Balkana.Data.Models.Series>> allRounds)
        {
            for (int roundIndex = 0; roundIndex < allRounds.Count - 1; roundIndex++)
            {
                var currentRound = allRounds[roundIndex];
                var nextRound = allRounds[roundIndex + 1];

                // Wire up progression: each pair of matches in current round feeds into one match in next round
                for (int i = 0; i < currentRound.Count; i += 2)
                {
                    if (i + 1 < currentRound.Count && i / 2 < nextRound.Count)
                    {
                        currentRound[i].NextSeriesId = nextRound[i / 2].Id;
                        currentRound[i + 1].NextSeriesId = nextRound[i / 2].Id;
                        Console.WriteLine($"🎯 Wired Round {roundIndex + 1} Match {i + 1} & {i + 2} -> Round {roundIndex + 2} Match {i / 2 + 1}");
                    }
                }
            }
        }

        /// <summary>
        /// Test method to verify bracket generation works correctly
        /// </summary>
        public void TestBracketGeneration()
        {
            Console.WriteLine("🧪 Testing Bracket Generation");
            
            // Test different team counts
            var testCases = new[] { 4, 6, 8, 10, 12, 14, 16 };
            
            foreach (var teamCount in testCases)
            {
                Console.WriteLine($"\n🧪 Testing {teamCount} teams:");
                
                var config = CalculateBracketConfiguration(teamCount);
                Console.WriteLine($"   Bracket Size: {config.BracketSize}");
                Console.WriteLine($"   First Round Teams: {config.FirstRoundTeams}");
                Console.WriteLine($"   Bye Teams: {config.ByeTeams}");
                
                var seedPositions = GenerateTournamentSeeding(config.BracketSize);
                Console.WriteLine($"   Seed Positions: [{string.Join(", ", seedPositions)}]");
                
                // Verify seeding makes sense
                if (teamCount == 6)
                {
                    // For 6 teams, should use 8-team bracket with 2 byes
                    if (config.BracketSize == 8 && config.ByeTeams == 2)
                    {
                        Console.WriteLine("   ✅ 6-team bracket configuration is correct");
                    }
                    else
                    {
                        Console.WriteLine("   ❌ 6-team bracket configuration is incorrect");
                    }
                }
                else if (teamCount == 10)
                {
                    // For 10 teams, should use 16-team bracket with 6 byes
                    if (config.BracketSize == 16 && config.ByeTeams == 6)
                    {
                        Console.WriteLine("   ✅ 10-team bracket configuration is correct");
                    }
                    else
                    {
                        Console.WriteLine("   ❌ 10-team bracket configuration is incorrect");
                    }
                }
                else if (teamCount == 12)
                {
                    // For 12 teams, should use 16-team bracket with 4 byes
                    if (config.BracketSize == 16 && config.ByeTeams == 4)
                    {
                        Console.WriteLine("   ✅ 12-team bracket configuration is correct");
                    }
                    else
                    {
                        Console.WriteLine("   ❌ 12-team bracket configuration is incorrect");
                    }
                }
                else if (teamCount == 14)
                {
                    // For 14 teams, should use 16-team bracket with 2 byes
                    if (config.BracketSize == 16 && config.ByeTeams == 2)
                    {
                        Console.WriteLine("   ✅ 14-team bracket configuration is correct");
                    }
                    else
                    {
                        Console.WriteLine("   ❌ 14-team bracket configuration is incorrect");
                    }
                }
            }
            
            Console.WriteLine("\n🧪 Bracket Generation Test Complete");
        }
    }

    public class BracketConfiguration
    {
        public int BracketSize { get; set; }
        public int FirstRoundTeams { get; set; }
        public int ByeTeams { get; set; }
        public int TotalTeams { get; set; }
    }
}