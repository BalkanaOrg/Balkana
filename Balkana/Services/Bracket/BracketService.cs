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
                return GenerateSingleElimination(teams, tournamentId, tournament.ShortName ?? $"Tournament {tournament.Id}");
            else
                return new DoubleEliminationBracketService(data, mapper)
                    .GenerateDoubleElimination(teams, tournamentId);
        }

        private List<Balkana.Data.Models.Series> GenerateSingleElimination(List<Team> teams, int tournamentId, string shortName)
        {
            var seriesList = new List<Balkana.Data.Models.Series>();

            int teamCount = teams.Count;
            int bracketSize = NextPowerOfTwo(teamCount);
            int byes = bracketSize - teamCount;

            // Step 1: assign seeds in bracket positions
            var positions = GenerateSeedPositions(bracketSize); // 0-based bracket slots
            var slots = new Team[bracketSize];

            int teamIndex = 0;
            for (int i = 0; i < bracketSize; i++)
            {
                if (i < byes)
                {
                    // top seeds get bye
                    slots[positions[i]] = null;
                }
                else
                {
                    slots[positions[i]] = teams[teamIndex++];
                }
            }

            int round = 1;
            while (slots.Length > 1)
            {
                int matchNumber = 1;
                int nextRoundSize = slots.Length / 2;
                var nextRoundSlots = new Team[nextRoundSize];

                for (int i = 0; i < slots.Length; i += 2)
                {
                    var teamA = slots[i];
                    var teamB = slots[i + 1];

                    if (teamA != null && teamB != null)
                    {
                        // normal match
                        seriesList.Add(NewSeries(
                            $"{shortName} - Round {round} Match {matchNumber}",
                            tournamentId,
                            teamA,
                            teamB,
                            round,
                            matchNumber,
                            BracketType.Upper
                        ));
                        nextRoundSlots[i / 2] = null; // winner placeholder
                    }
                    else if (teamA != null || teamB != null)
                    {
                        // bye -> auto-advance
                        nextRoundSlots[i / 2] = teamA ?? teamB;

                        // optionally include a BYE match
                        seriesList.Add(NewSeries(
                            $"{shortName} - Round {round} Match {matchNumber} (BYE)",
                            tournamentId,
                            teamA,
                            teamB,
                            round,
                            matchNumber,
                            BracketType.Upper
                        ));
                    }
                    else
                    {
                        // both null -> placeholder
                        nextRoundSlots[i / 2] = null;
                    }

                    matchNumber++;
                }

                slots = nextRoundSlots;
                round++;
            }

            return seriesList;
        }


        private int[] GenerateSeedPositions(int bracketSize)
        {
            if (bracketSize == 1) return new[] { 0 };

            var smaller = GenerateSeedPositions(bracketSize / 2);
            var result = new int[bracketSize];

            for (int i = 0; i < smaller.Length; i++)
            {
                result[i] = smaller[i];
                result[bracketSize - 1 - i] = bracketSize - 1 - smaller[i];
            }

            return result;
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

        private List<int?> GenerateSeeding(int bracketSize, int teamCount)
        {
            // Recursive seeding
            List<int?> seeds = new List<int?>(new int?[bracketSize]);
            void PlaceSeed(int seed, int start, int end)
            {
                if (start == end)
                {
                    seeds[start] = seed <= teamCount ? seed : (int?)null;
                    return;
                }
                int mid = (start + end) / 2;
                PlaceSeed(seed, start, mid);
                PlaceSeed(bracketSize + 1 - seed, mid + 1, end);
            }

            PlaceSeed(1, 0, bracketSize - 1);
            return seeds;
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