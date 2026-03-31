namespace Balkana.Data.Models
{
    public class BalkanaAwardResult
    {
        public int Id { get; set; }

        public int BalkanaAwardsId { get; set; }
        public BalkanaAwardsEvent BalkanaAwards { get; set; }

        public int CategoryId { get; set; }
        public BalkanaAwardCategory Category { get; set; }

        public int Rank { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }
    }
}

