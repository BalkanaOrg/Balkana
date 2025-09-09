namespace Balkana.Models.Community
{
    public class CommunityTeamMemberModel
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; } // Player, Substitute, Coach
        public bool IsApproved { get; set; }
    }
}
