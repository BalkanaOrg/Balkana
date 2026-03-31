namespace Balkana.Models.Community
{
    public class CommunityTeamInspectionViewModel
    {
        public int TeamId { get; set; }
        public string Tag { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? LogoUrl { get; set; }
        public string GameName { get; set; } = "";
        public bool TeamApproved { get; set; }

        public string? RequiredAccountType { get; set; } // FaceIt / Riot

        public List<CommunityTeamInspectionMemberViewModel> Members { get; set; } = new();
    }

    public class CommunityTeamInspectionMemberViewModel
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";

        public bool MemberApproved { get; set; }
        public bool HasPlayerLinked { get; set; }
        public bool HasRequiredLinkedAccount { get; set; }
        public string? RequiredAccountDisplayName { get; set; }
    }
}

