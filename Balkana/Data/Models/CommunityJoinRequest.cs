namespace Balkana.Data.Models
{
    public class CommunityJoinRequest
    {
        public int Id { get; set; }
        public int CommunityTeamId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public CommunityTeam CommunityTeam { get; set; }
        public ApplicationUser User { get; set; }
    }
}
