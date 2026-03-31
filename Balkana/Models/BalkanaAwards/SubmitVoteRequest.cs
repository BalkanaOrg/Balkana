namespace Balkana.Models.BalkanaAwards
{
    public class SubmitVoteRequest
    {
        public int CategoryId { get; set; }
        public List<SubmitVoteItem> Items { get; set; } = new();
    }

    public class SubmitVoteItem
    {
        public int Rank { get; set; }
        public int? PlayerId { get; set; }
        public int? TeamId { get; set; }
        public int? TournamentId { get; set; }
        public string? CandidateUserId { get; set; }
    }
}

