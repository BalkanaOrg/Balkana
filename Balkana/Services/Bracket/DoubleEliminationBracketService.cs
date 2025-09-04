using AutoMapper;
using Balkana.Data;
using Balkana.Data.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Balkana.Services.Bracket
{
    public class DoubleEliminationBracketService
    {
        private readonly ApplicationDbContext data;
        private readonly AutoMapper.IConfigurationProvider mapper;

        public DoubleEliminationBracketService(ApplicationDbContext data, IMapper mapper)
        {
            this.mapper = mapper.ConfigurationProvider;
            this.data = data;
        }

        public List<Balkana.Data.Models.Series> GenerateDoubleElimination(List<Team> teams, int tournamentId)
        {
            int teamCount = teams.Count;
            if (teamCount != 4 && teamCount != 8 && teamCount != 16)
                throw new InvalidOperationException("Only 4, 8, or 16 teams supported for now");

            var seededTeams = data.TournamentTeams
                .Where(tt => tt.TournamentId == tournamentId)
                .OrderBy(tt => tt.Seed)
                .Select(tt => tt.Team)   // gets the actual Team entity
                .ToList();
            var seriesList = new List<Balkana.Data.Models.Series>();

            // --- UPPER BRACKET ---
            int round = 1;
            int matchId = 1;

            // First round UB pairings
            for (int i = 0; i < teamCount / 2; i++)
            {
                seriesList.Add(new Balkana.Data.Models.Series
                {
                    Name = $"UB Round {round} - Match {i + 1}",
                    TournamentId = tournamentId,
                    TeamAId = seededTeams[i].Id,
                    TeamBId = seededTeams[teamCount - 1 - i].Id,
                    Round = round,
                    Position = i + 1,
                    Bracket = BracketType.Upper
                });
            }

            // Next UB rounds
            int matchesInRound = teamCount / 2;
            while (matchesInRound > 1)
            {
                round++;
                matchesInRound /= 2;
                for (int i = 0; i < matchesInRound; i++)
                {
                    seriesList.Add(new Balkana.Data.Models.Series
                    {
                        Name = $"UB Round {round} - Match {i + 1}",
                        TournamentId = tournamentId,
                        Round = round,
                        Position = i + 1,
                        Bracket = BracketType.Upper
                    });
                }
            }

            // --- LOWER BRACKET ---
            // The LB gets complicated; losers from UB Round 1 drop here.
            int lbRound = 1;
            int lbMatches = teamCount / 2;
            while (lbMatches > 0)
            {
                for (int i = 0; i < lbMatches; i++)
                {
                    seriesList.Add(new Balkana.Data.Models.Series
                    {
                        Name = $"LB Round {lbRound} - Match {i + 1}",
                        TournamentId = tournamentId,
                        Round = lbRound,
                        Position = i + 1,
                        Bracket = BracketType.Lower
                    });
                }

                lbRound++;
                lbMatches = (lbMatches == 1) ? 0 : lbMatches / 2;
            }

            // --- GRAND FINAL ---
            seriesList.Add(new Balkana.Data.Models.Series
            {
                Name = "Grand Final",
                TournamentId = tournamentId,
                Round = 1,
                Position = 1,
                Bracket = BracketType.GrandFinal
            });

            // TODO: Wire up NextSeriesId for UB→UB, UB→LB, LB→LB, LB→GF

            return seriesList;
        }
    }
}
