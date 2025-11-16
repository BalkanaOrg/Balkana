namespace Balkana.Data.Models
{
    public class PlayerSocials
    {
        public int Id { get; set; }
        public string Type { get; set; } // Twitch, YouTube, Twitter, etc.
        public string Url { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }

    }
}
