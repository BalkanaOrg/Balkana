using System.ComponentModel.DataAnnotations;
using Balkana.Data.Models;
using Microsoft.AspNetCore.Http;

namespace Balkana.Models.Accounts
{
    public class GoogleRegisterViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required, Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required, Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, Display(Name = "Nationality")]
        public int NationalityId { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile ProfilePicture { get; set; }

        // Hidden fields to store Google info (not required for validation)
        public string GoogleId { get; set; }
        public string GoogleEmail { get; set; }
        public string GoogleFirstName { get; set; }
        public string GoogleLastName { get; set; }
        public string GoogleProfilePicture { get; set; }

        // Nationalities list for dropdown (not required for validation, just for display)
        public List<Nationality> Nationalities { get; set; }
    }
}

