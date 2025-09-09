namespace Balkana.Data.Models
{
    public class CommunityTeamTransfer
    {
        public int Id { get; set; }

        public int CommunityTeamId { get; set; }
        public CommunityTeam CommunityTeam { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime TransferDate { get; set; } = DateTime.UtcNow;

        // Role/position in the community team (could map to TeamPosition if promoted)
        public int? PositionId { get; set; }
        public TeamPosition Position { get; set; }
    }
}
