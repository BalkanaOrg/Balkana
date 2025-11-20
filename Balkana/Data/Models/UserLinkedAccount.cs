using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models
{
    public class UserLinkedAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } // Discord, FaceIt, Instagram, Facebook, YouTube, Twitch, etc.

        [Required]
        [StringLength(500)]
        public string Identifier { get; set; } // UUID for Discord/FaceIt, username/tag for social media
    }
}

