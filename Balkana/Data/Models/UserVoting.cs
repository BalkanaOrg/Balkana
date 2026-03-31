namespace Balkana.Data.Models
{
    public class UserVoting
    {
        public int Id { get; set; }

        public int BalkanaAwardsId { get; set; }
        public BalkanaAwardsEvent BalkanaAwards { get; set; }

        public int CategoryId { get; set; }
        public BalkanaAwardCategory Category { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserVotingItem> Items { get; set; } = new List<UserVotingItem>();
    }
}

