namespace Balkana.Models.Match
{
    public class MatchDetailsViewModel
    {
        public int MatchId { get; set; }
        public string SeriesName { get; set; }
        public DateTime MatchDate { get; set; }
        public List<PlayerStatViewModel> PlayerStats { get; set; } = new List<PlayerStatViewModel>();
    }
}
