namespace Balkana.Services.Teams.Models
{
    public class TeamTournamentServiceModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public string TournamentShortName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PrizePool { get; set; }
        public int Seed { get; set; }
        public int? Placement { get; set; }
        public int? PointsAwarded { get; set; }
        public string OrganizerName { get; set; }
        public string BannerUrl { get; set; }
        public bool IsCompleted { get; set; }
    }
}
