namespace Balkana.Services.Teams.Models
{
    using System.ComponentModel.DataAnnotations;
    using Balkana.Models.Teams;
    using static Balkana.Data.DataConstants;

    public class TeamFormModel : ITeamModel
    {
        [Required]
        [Display(Name = "Team full name")]
        [StringLength(TeamFullNameMaxLength, MinimumLength = TeamFullNameMinLength)]
        public string FullName { get; init; }

        [Required]
        [Display(Name = "Team tag")]
        [StringLength(TeamTagMaxLength, MinimumLength = TeamTagMinLength)]
        public string Tag { get; init; }

        [Display(Name = "Team Logo")]
        public IFormFile? LogoFile { get; init; }

        public string? LogoPath { get; set; } // stored in DB (relative path)

        public int GameId { get; init; }

        public int YearFounded { get; init; }

        public IEnumerable<TeamGameServiceModel> CategoriesGames { get; set; }
    }
}
