using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Models.Match
{
    public class MatchCreateViewModel : MatchBaseViewModel
    {
        public int SeriesId { get; set; }
        public List<SelectListItem> SeriesList { get; set; } = new List<SelectListItem>();
        public int MapId { get; set; }
        public List<SelectListItem> MapsList { get; set; } = new List<SelectListItem>();
        public string VOD { get; set; }
    }
}
