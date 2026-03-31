namespace Balkana.Data.Models
{
    public class BalkanaAwardEligibilityPlayer
    {
        public int BalkanaAwardsId { get; set; }
        public BalkanaAwardsEvent BalkanaAwards { get; set; }

        public int CategoryId { get; set; }
        public BalkanaAwardCategory Category { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }
    }
}

