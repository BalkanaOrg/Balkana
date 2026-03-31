using Balkana.Models.Players;
using Balkana.Services.Nationality;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Services.Players.Models
{
    using static Data.DataConstants;

    public class PlayerFormModel : IPlayerModel
    {
        /// <summary>0 when creating a new player.</summary>
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nickname")]
        [StringLength(NameMaxLength, MinimumLength = NameMinLength)]
        public string Nickname { get; set; } = "";

        [Display(Name = "First name")]
        [StringLength(NameMaxLength)]
        public string? FirstName { get; set; }

        [Display(Name = "Last name")]
        [StringLength(NameMaxLength)]
        public string? LastName { get; set; }

        [Required]
        [Display(Name = "Nationality")]
        public int NationalityId { get; set; }

        [ValidateNever]
        public IEnumerable<PlayerNationalityServiceModel> Nationalities { get; set; } = Array.Empty<PlayerNationalityServiceModel>();

        [ValidateNever]
        [Display(Name = "Profile picture")]
        public IFormFile? PictureFile { get; set; }
    }
}
