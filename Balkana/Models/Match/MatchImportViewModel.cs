using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Models.Match
{
    public class MatchImportViewModel
    {
        // Source of the match, e.g., "RIOT" or "FACEIT"
        public string Source { get; set; }

        // Profile ID of the player to import matches for
        public string ProfileId { get; set; }

        public string MatchId { get; set; }

        // Selected tournament/series
        public int SelectedTournamentId { get; set; }
        public int SelectedSeriesId { get; set; }

        // Dropdown for selecting tournament
        public List<SelectListItem> Tournaments { get; set; } = new List<SelectListItem>();

        // Dropdown for selecting map (optional, if you want user to pick map manually)
        public int? SelectedMapId { get; set; }
        public List<SelectListItem> Maps { get; set; } = new List<SelectListItem>();

        // Optional: manually override competition type
        public string CompetitionType { get; set; }

        // Optional: date override
        public DateTime? PlayedAt { get; set; }

        // Optional: Team overrides
        public int? TeamAId { get; set; }
        public int? TeamBId { get; set; }

        // For UI display: external IDs
        public string ExternalMatchId { get; set; }
        public string MapExternalId { get; set; }

        public List<SelectListItem> Clubs { get; set; } = new();  // 👈 new
        public int? SelectedClubId { get; set; }                  // 👈 new
    }
}
