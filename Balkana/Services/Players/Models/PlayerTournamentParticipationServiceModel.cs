namespace Balkana.Services.Players.Models
{
    public class PlayerTournamentParticipationServiceModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public string TournamentShortName { get; set; }
        public string GameName { get; set; }
        public string OrganizerName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PrizePool { get; set; }
        public string BannerUrl { get; set; }
        
        // Team information
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TeamTag { get; set; }
        public string TeamLogoUrl { get; set; }
        
        // Placement information
        public int? Placement { get; set; }
        public int? PointsAwarded { get; set; }
        public decimal? PrizeWon { get; set; }
        
        // Series/Match information
        public int TotalSeries { get; set; }
        public int SeriesWins { get; set; }
        public int SeriesLosses { get; set; }
        public int TotalMatches { get; set; }
        public int MatchWins { get; set; }
        public int MatchLosses { get; set; }
        
        public double SeriesWinRate => TotalSeries > 0 ? (double)SeriesWins / TotalSeries * 100 : 0;
        public double MatchWinRate => TotalMatches > 0 ? (double)MatchWins / TotalMatches * 100 : 0;
    }
}
