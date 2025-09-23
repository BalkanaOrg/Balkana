namespace Balkana.Services.Teams.Models
{
    using Balkana.Models.Teams;
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
    using System.ComponentModel.DataAnnotations;
    using static Balkana.Data.DataConstants;

    public class TeamFormModel : ITeamModel
    {
        [Required]
        [Display(Name = "Team full name")]
        [StringLength(TeamFullNameMaxLength, MinimumLength = TeamFullNameMinLength)]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Team tag")]
        [StringLength(TeamTagMaxLength, MinimumLength = TeamTagMinLength)]
        public string Tag { get; set; }

        [Display(Name = "Team Logo")]
        public IFormFile? LogoFile { get; set; }

        public string? LogoPath { get; set; } // stored in DB (relative path)

        public int GameId { get; set; }

        public int YearFounded { get; set; }

        [ValidateNever]
        public IEnumerable<TeamGameServiceModel> CategoriesGames { get; set; }
    }
}
