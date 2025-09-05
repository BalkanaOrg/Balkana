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

        public List<Balkana.Data.Models.Series> Generate8TeamDoubleElimination(List<Team> teams, int tournamentId, string shortName)
        {
            if (teams.Count != 8)
                throw new InvalidOperationException("8 teams required");

            var seriesList = new List<Balkana.Data.Models.Series>();

            // --- UB ROUND 1 ---
            var ub1 = NewSeries($"{shortName} - UB Round 1 Match 1", tournamentId, teams[0], teams[7], 1, 1, BracketType.Upper);
            var ub2 = NewSeries($"{shortName} - UB Round 1 Match 2", tournamentId, teams[3], teams[4], 1, 2, BracketType.Upper);
            var ub3 = NewSeries($"{shortName} - UB Round 1 Match 3", tournamentId, teams[1], teams[6], 1, 3, BracketType.Upper);
            var ub4 = NewSeries($"{shortName} - UB Round 1 Match 4", tournamentId, teams[2], teams[5], 1, 4, BracketType.Upper);

            // --- UB SEMIS ---
            var ub5 = NewSeries($"{shortName} - UB Semifinal 1", tournamentId, null, null, 2, 1, BracketType.Upper);
            var ub6 = NewSeries($"{shortName} - UB Semifinal 2", tournamentId, null, null, 2, 2, BracketType.Upper);

            // --- UB Final ---
            var ub7 = NewSeries($"{shortName} - UB Final", tournamentId, null, null, 3, 1, BracketType.Upper);

            // --- LB ROUND 1 ---
            var lb1 = NewSeries($"{shortName} - LB Round 1 Match 1", tournamentId, null, null, 1, 1, BracketType.Lower);
            var lb2 = NewSeries($"{shortName} - LB Round 1 Match 2", tournamentId, null, null, 1, 2, BracketType.Lower);

            // --- LB ROUND 2 ---
            var lb3 = NewSeries($"{shortName} - LB Round 2 Match 3", tournamentId, null, null, 2, 1, BracketType.Lower);
            var lb4 = NewSeries($"{shortName} - LB Round 2 Match 4", tournamentId, null, null, 2, 2, BracketType.Lower);

            // --- LB SEMI & FINAL ---
            var lb5 = NewSeries($"{shortName} - LB Semifinal", tournamentId, null, null, 3, 1, BracketType.Lower);
            var lb6 = NewSeries($"{shortName} - LB Final", tournamentId, null, null, 4, 1, BracketType.Lower);

            // --- Grand Final ---
            var gf = NewSeries($"{shortName} - Grand Final", tournamentId, null, null, 1, 1, BracketType.GrandFinal);

            // --- Add all series ---
            seriesList.AddRange(new[] { ub1, ub2, ub3, ub4, ub5, ub6, ub7, lb1, lb2, lb3, lb4, lb5, lb6, gf });

            // --- Wire NextSeries relationships ---
            ub1.NextSeries = ub5; ub2.NextSeries = ub5;
            ub3.NextSeries = ub6; ub4.NextSeries = ub6;
            ub5.NextSeries = ub7; ub6.NextSeries = ub7;
            ub7.NextSeries = gf;

            lb1.NextSeries = lb3;
            lb2.NextSeries = lb4;
            lb3.NextSeries = lb5;
            lb4.NextSeries = lb5;
            lb5.NextSeries = lb6;
            lb6.NextSeries = gf;

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
    }
}
