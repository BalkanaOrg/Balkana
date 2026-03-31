namespace Balkana.Data.Models
{
    public class BalkanaAwardsEvent
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime? VotingOpensAt { get; set; }
        public DateTime? VotingClosesAt { get; set; }

        public ICollection<BalkanaAwardEligibilityPlayer> EligiblePlayers { get; set; } = new List<BalkanaAwardEligibilityPlayer>();
        public ICollection<UserVoting> Votes { get; set; } = new List<UserVoting>();
        public ICollection<BalkanaAwardResult> Results { get; set; } = new List<BalkanaAwardResult>();
    }
}

