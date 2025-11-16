namespace Balkana.Data.Models
{
    public class TeamSocials
    {
        public int Id { get; set; }
        public string Type { get; set; } // Twitter, Facebook, Instagram, YouTube, Twitch
        public string Url { get; set; }
        public int TeamId { get; set; }
        public Team Team { get; set; }
    }
}
