namespace Balkana.Services.Players.Models
{
    public class PlayerMatchHistoryServiceModel
    {
        public string GameName { get; set; }
        public string Source { get; set; }
        public List<PlayerTournamentGroupServiceModel> TournamentGroups { get; set; } = new();
    }
    
    public class PlayerTournamentGroupServiceModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public string TournamentShortName { get; set; }
        public string OrganizerName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string BannerUrl { get; set; }
        public List<PlayerSeriesGroupServiceModel> SeriesGroups { get; set; } = new();
    }
    
    public class PlayerSeriesGroupServiceModel
    {
        public int SeriesId { get; set; }
        public string SeriesName { get; set; }
        public DateTime DatePlayed { get; set; }
        public bool IsSeriesWinner { get; set; }
        
        // Player's team information
        public string? PlayerTeamName { get; set; }
        public string? PlayerTeamTag { get; set; }
        public string? PlayerTeamLogo { get; set; }
        
        // Opponent team information
        public string? OpponentTeamName { get; set; }
        public string? OpponentTeamTag { get; set; }
        public string? OpponentTeamLogo { get; set; }
        
        public List<PlayerMatchStatsServiceModel> Matches { get; set; } = new();
        public int MatchWins { get; set; }
        public int MatchLosses { get; set; }
    }
}
