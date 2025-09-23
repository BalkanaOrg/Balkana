using Balkana.Models.Players;
using Balkana.Services.Nationality;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Services.Players.Models
{
    using static Data.DataConstants;

    public class PlayerFormModel : IPlayerModel
    {
        [Required]
        [Display(Name = "Nickname")]
        [StringLength(NameMaxLength, MinimumLength = NameMinLength)]
        public string Nickname { get; init; }

        [Required]
        [Display(Name = "First name")]
        [StringLength(NameMaxLength, MinimumLength = NameMinLength)]
        public string FirstName { get; init; }

        [Required]
        [Display(Name = "Last name")]
        [StringLength(NameMaxLength, MinimumLength = NameMinLength)]
        public string LastName { get; init; }

        [Required]
        [Display(Name = "Nationality")]
        public int NationalityId { get; init; }

        [ValidateNever]
        public IEnumerable<PlayerNationalityServiceModel> Nationalities { get; set; }
    }
}
