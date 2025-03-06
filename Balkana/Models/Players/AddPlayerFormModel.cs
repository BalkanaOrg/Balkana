namespace Balkana.Models.Players
{
    using Balkana.Data.Models;
    using Balkana.Services.Nationality;
    using System.ComponentModel.DataAnnotations;
    using static Data.DataConstants;

    public class AddPlayerFormModel
    {
        [Required]
        [Display(Name = "Player's IGN (In-game nickname)")]
        [StringLength(TeamFullNameMaxLength, MinimumLength = 1)]
        public string Nickname { get; set; }

        [Required]
        [Display(Name = "Player's first name")]
        [StringLength(NameMaxLength, MinimumLength = NameMinLength)]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Player's last name")]
        [StringLength(NameMaxLength, MinimumLength = NameMinLength)]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Player's nationality")]
        public int NationalityId { get; set; }

        public IEnumerable<PlayerNationalityServiceModel> Nationalities { get; set; }
    }
}
