using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Models.Tournaments
{
    public class CreateRiotTournamentViewModel
    {
        [Required]
        [Display(Name = "Tournament Name")]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Region")]
        public string Region { get; set; } = "EUW1";

        [Display(Name = "Link to Internal Tournament (Optional)")]
        public int? TournamentId { get; set; }

        // If no provider exists, we'll create one automatically
        public int? ProviderId { get; set; }

        public List<SelectListItem> AvailableRegions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "EUW1", Text = "Europe West" },
            new SelectListItem { Value = "EUNE1", Text = "Europe Nordic & East" },
            new SelectListItem { Value = "NA1", Text = "North America" },
            new SelectListItem { Value = "BR1", Text = "Brazil" },
            new SelectListItem { Value = "LA1", Text = "Latin America North" },
            new SelectListItem { Value = "LA2", Text = "Latin America South" },
            new SelectListItem { Value = "OC1", Text = "Oceania" },
            new SelectListItem { Value = "RU", Text = "Russia" },
            new SelectListItem { Value = "TR1", Text = "Turkey" },
            new SelectListItem { Value = "JP1", Text = "Japan" },
            new SelectListItem { Value = "KR", Text = "Korea" }
        };

        public List<SelectListItem> InternalTournaments { get; set; } = new List<SelectListItem>();
    }
}

