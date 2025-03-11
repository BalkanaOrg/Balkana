using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Models.Match
{
    public class MatchDetailsViewModel : MatchBaseViewModel
    {
        public int MatchId { get; set; }
        public string SeriesName { get; set; }
        public DateTime MatchDate { get; set; }
        public List<PlayerStatViewModel> PlayerStats { get; set; }
        public List<SelectListItem> PlayersList { get; set; }
    }
}
