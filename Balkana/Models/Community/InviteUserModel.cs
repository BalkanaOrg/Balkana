using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Community
{
    public class InviteUserModel
    {
        [Required]
        public string InvitedUserId { get; set; } // FK to ApplicationUser

        [Required]
        public int CommunityTeamId { get; set; }
    }
}
