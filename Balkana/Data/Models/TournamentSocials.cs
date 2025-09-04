namespace Balkana.Data.Models
{
    public class TournamentSocials
    {
        public int Id { get; set; }
        public string Type { get; set; } // "TWITTER", "FACEBOOK", "INSTAGRAM", "YOUTUBE", "DISCORD"
        public string Url { get; set; }
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }
    }
}
