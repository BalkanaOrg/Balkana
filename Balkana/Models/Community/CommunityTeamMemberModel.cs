namespace Balkana.Models.Community
{
    public class CommunityTeamMemberModel
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; } // Player, Substitute, Coach
        public bool IsApproved { get; set; }

        public bool HasPlayerLinked { get; set; }
        public bool HasRequiredLinkedAccount { get; set; }
        public string? RequiredAccountType { get; set; } // FaceIt / Riot
        public string? RequiredAccountDisplayName { get; set; }
    }
}
