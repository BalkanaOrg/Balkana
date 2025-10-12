namespace Balkana.Services.Teams.Models
{
    public class TeamStatisticsServiceModel
    {
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate { get; set; }
        public int TotalTournaments { get; set; }
        public int TournamentWins { get; set; }
        public int Top3Finishes { get; set; }
        public int TotalPoints { get; set; }
        public decimal TotalPrizeMoney { get; set; }
        public Dictionary<string, MapStatistics> MapStats { get; set; } = new();
    }

    public class MapStatistics
    {
        public string MapName { get; set; }
        public string MapImageUrl { get; set; }
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate { get; set; }
    }
}
