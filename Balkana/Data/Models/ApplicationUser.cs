using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Foreign key to Nationality
        public int NationalityId { get; set; }
        [ForeignKey(nameof(NationalityId))]
        public Nationality Nationality { get; set; }

        public string ProfilePictureUrl { get; set; }
    }
}
