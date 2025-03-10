using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Models.Match
{
    public class MatchCreateViewModel
    {
        public int SeriesId { get; set; }
        public List<SelectListItem> SeriesList { get; set; } = new List<SelectListItem>();
        public DateTime MatchDate { get; set; }
    }
}
