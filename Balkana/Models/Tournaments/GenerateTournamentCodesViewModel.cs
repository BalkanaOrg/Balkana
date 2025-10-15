using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Models.Tournaments
{
    public class GenerateTournamentCodesViewModel
    {
        [Required]
        public int RiotTournamentId { get; set; }

        public string TournamentName { get; set; }

        [Required]
        [Range(1, 100)]
        [Display(Name = "Number of Codes to Generate")]
        public int Count { get; set; } = 1;

        [Display(Name = "Series (Optional)")]
        public int? SeriesId { get; set; }

        [Display(Name = "Team A (Optional)")]
        public int? TeamAId { get; set; }

        [Display(Name = "Team B (Optional)")]
        public int? TeamBId { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Map Type")]
        public string MapType { get; set; } = "SUMMONERS_RIFT";

        [Required]
        [Display(Name = "Pick Type")]
        public string PickType { get; set; } = "TOURNAMENT_DRAFT";

        [Required]
        [Display(Name = "Spectator Type")]
        public string SpectatorType { get; set; } = "ALL";

        [Required]
        [Range(1, 5)]
        [Display(Name = "Team Size")]
        public int TeamSize { get; set; } = 5;

        [Display(Name = "Metadata (Optional)")]
        [StringLength(1000)]
        public string Metadata { get; set; }

        public List<SelectListItem> MapTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "SUMMONERS_RIFT", Text = "Summoner's Rift" },
            new SelectListItem { Value = "HOWLING_ABYSS", Text = "Howling Abyss (ARAM)" }
        };

        public List<SelectListItem> PickTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "TOURNAMENT_DRAFT", Text = "Tournament Draft" },
            new SelectListItem { Value = "BLIND_PICK", Text = "Blind Pick" },
            new SelectListItem { Value = "DRAFT_MODE", Text = "Draft Mode" },
            new SelectListItem { Value = "ALL_RANDOM", Text = "All Random" }
        };

        public List<SelectListItem> SpectatorTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "ALL", Text = "All (Anyone can spectate)" },
            new SelectListItem { Value = "LOBBYONLY", Text = "Lobby Only" },
            new SelectListItem { Value = "NONE", Text = "None (No spectators)" }
        };

        public List<SelectListItem> AvailableSeries { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableTeams { get; set; } = new List<SelectListItem>();
    }
}

