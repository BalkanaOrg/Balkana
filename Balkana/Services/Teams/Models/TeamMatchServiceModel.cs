namespace Balkana.Services.Teams.Models
{
    public class TeamMatchServiceModel
    {
        public int MatchId { get; set; }
        public int SeriesId { get; set; }
        public int? TournamentId { get; set; }
        public string TournamentName { get; set; }
        public string? TournamentShortName { get; set; }
        public int? OpponentTeamId { get; set; }
        public DateTime PlayedAt { get; set; }
        public string OpponentName { get; set; }
        public string OpponentTag { get; set; }
        public string OpponentLogoUrl { get; set; }
        public bool IsWin { get; set; }
        public bool IsCompleted { get; set; }
        public string MapName { get; set; }
        public string Source { get; set; }
        public string ExternalMatchId { get; set; }

        public bool IsLeagueOfLegends { get; set; }
        public string? DDragonVersion { get; set; }
        public IReadOnlyList<LoLChampionRowVm> OurChampions { get; set; } = Array.Empty<LoLChampionRowVm>();
        public IReadOnlyList<LoLChampionRowVm> TheirChampions { get; set; } = Array.Empty<LoLChampionRowVm>();
    }

    public class LoLChampionRowVm
    {
        public int ChampionId { get; set; }
        public string ChampionName { get; set; } = "";
    }
}
