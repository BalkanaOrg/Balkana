using Balkana.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Accounts
{
    public class EditProfileViewModel
    {
        [Required]
        public string Id { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Nationality")]
        public int NationalityId { get; set; }

        public List<Nationality> Nationalities { get; set; } = new List<Nationality>();
    }
}
