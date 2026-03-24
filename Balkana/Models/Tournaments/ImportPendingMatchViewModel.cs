using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Models.Tournaments
{
    public class ImportPendingMatchViewModel
    {
        public int PendingMatchId { get; set; }
        public string MatchId { get; set; } = "";
        public string? TournamentCode { get; set; }
        public int? SuggestedSeriesId { get; set; }
        public int SelectedSeriesId { get; set; }
        public List<SelectListItem> Series { get; set; } = new();
    }
}
