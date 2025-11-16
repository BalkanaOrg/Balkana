namespace Balkana.Data.Models
{
    public class CommunityTeamMember
    {
        // Composite key configured in DbContext: CommunityTeamId + UserId
        public int CommunityTeamId { get; set; }
        public CommunityTeam CommunityTeam { get; set; }

        // the user from Identity
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public CommunityMemberRole Role { get; set; } = CommunityMemberRole.Player;

        // Player approval state (each user needs moderator approval to be promoted into official Player table)
        public bool IsApproved { get; set; } = false;
        public int? ApprovedById { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
