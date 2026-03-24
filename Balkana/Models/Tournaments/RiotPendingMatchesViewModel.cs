namespace Balkana.Models.Tournaments
{
    public class RiotPendingMatchesViewModel
    {
        public List<RiotPendingMatchItemViewModel> Items { get; set; } = new();
    }

    public class RiotPendingMatchItemViewModel
    {
        public int Id { get; set; }
        public string MatchId { get; set; } = "";
        public string? TournamentCode { get; set; }
        public string? LinkedCode { get; set; }
        public int? LinkedSeriesId { get; set; }
        public Balkana.Data.Models.RiotPendingMatchStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
