using Balkana.Data.Models;
using Balkana.Services.Tournaments.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Tournaments
{
    public class TournamentFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string ShortName { get; set; }
        [Required]
        public int OrganizerId { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now;

        [Display(Name = "Team Logo")]
        public IFormFile? LogoFile { get; set; }

        public decimal PrizePool { get; set; } = 0;

        [Display(Name = "Points Configuration (JSON)")]
        public string PointsConfiguration { get; set; } = "";

        [Display(Name = "Prize Configuration (JSON)")]
        public string PrizeConfiguration { get; set; } = "";

        public string? BannerUrl { get; set; }
        public EliminationType EliminationType { get; set; }  // single or double

        public int GameId { get; set; }

        public IEnumerable<TournamentGamesServiceModel>? Games { get; set; }
        public IEnumerable<TournamentOrganizersServiceModel>? Organizers { get; set; }
        public IEnumerable<SelectListItem>? EliminationTypes { get; set; }

        public List<int> SelectedTeamIds { get; set; } = new();
        public List<TeamSelectItem> AvailableTeams { get; set; } = new();
    }
}
