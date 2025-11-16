namespace Balkana.Services.Teams.Models
{
    public class TeamMatchServiceModel
    {
        public int MatchId { get; set; }
        public int SeriesId { get; set; }
        public string TournamentName { get; set; }
        public DateTime PlayedAt { get; set; }
        public string OpponentName { get; set; }
        public string OpponentTag { get; set; }
        public string OpponentLogoUrl { get; set; }
        public bool IsWin { get; set; }
        public bool IsCompleted { get; set; }
        public string MapName { get; set; }
        public string Source { get; set; }
        public string ExternalMatchId { get; set; }
    }
}
