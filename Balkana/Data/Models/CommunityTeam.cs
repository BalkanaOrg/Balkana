using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    public class CommunityTeam
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Tag { get; set; }           // short tag

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; }      // full team name

        public string LogoUrl { get; set; }

        public int GameId { get; set; }
        public Game Game { get; set; }

        // Approval state
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? ApprovedById { get; set; }   // Admin user id (Identity user id) - optional
        public string? ApprovedBy { get; set; }   // username or id string for quick ref
        public DateTime? ApprovedAt { get; set; }

        public ICollection<CommunityTeamMember> Members { get; set; } = new List<CommunityTeamMember>();
        public ICollection<CommunityInvite> Invites { get; set; } = new List<CommunityInvite>();
        public ICollection<CommunityTeamTransfer> Transfers { get; set; } = new List<CommunityTeamTransfer>();
    }
}
