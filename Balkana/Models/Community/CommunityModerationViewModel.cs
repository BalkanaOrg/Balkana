namespace Balkana.Models.Community
{
    public class CommunityModerationViewModel
    {
        public List<CommunityModerationTeamItemViewModel> Teams { get; set; } = new();
    }

    public class CommunityModerationTeamItemViewModel
    {
        public int TeamId { get; set; }
        public string Tag { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? LogoUrl { get; set; }
        public string GameName { get; set; } = "";

        public bool TeamApproved { get; set; }
        public int TotalMembers { get; set; }
        public int ApprovedMembers { get; set; }
        public int UnapprovedMembers { get; set; }

        public string? RequiredAccountType { get; set; } // FaceIt / Riot
        public int MissingRequiredAccounts { get; set; }
        public int MissingPlayerLinks { get; set; }
    }
}

