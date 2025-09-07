namespace Balkana.Data.Models
{
    public class CorePlayer
    {
        public int CoreId { get; set; }
        public Core Core { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }
    }
}
