namespace Balkana.Data.Models
{
    public class CommunityInvite
    {
        public int Id { get; set; }

        public int CommunityTeamId { get; set; }
        public CommunityTeam CommunityTeam { get; set; }

        // inviter is the captain's UserId (string)
        public string InviterUserId { get; set; }
        public ApplicationUser InviterUser { get; set; }

        // invitee is the invited user's UserId
        public string InviteeUserId { get; set; }
        public ApplicationUser InviteeUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        public bool IsAccepted { get; set; } = false;
        public DateTime? AcceptedAt { get; set; }

        public bool IsCancelled { get; set; } = false;
    }
}
