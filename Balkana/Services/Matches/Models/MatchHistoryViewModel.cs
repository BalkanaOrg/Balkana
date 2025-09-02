namespace Balkana.Services.Matches.Models
{
    public class MatchHistoryViewModel
    {
        public string MatchId { get; set; }
        public bool ExistsInDb { get; set; }
        public string Source { get; set; }
    }
}
