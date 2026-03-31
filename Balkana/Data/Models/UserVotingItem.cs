namespace Balkana.Data.Models
{
    public class UserVotingItem
    {
        public int Id { get; set; }

        public int UserVotingId { get; set; }
        public UserVoting UserVoting { get; set; }

        public int Rank { get; set; }

        public int? PlayerId { get; set; }
        public Player Player { get; set; }

        public int? TeamId { get; set; }
        public Team Team { get; set; }

        public int? TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public string CandidateUserId { get; set; }
        public ApplicationUser CandidateUser { get; set; }
    }
}

