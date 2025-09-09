namespace Balkana.Models.Community
{
    public class CommunityTeamDetailsModel
    {
        public int Id { get; set; }
        public string Tag { get; set; }
        public string FullName { get; set; }
        public string LogoUrl { get; set; }
        public string CaptainId { get; set; }
        public string CaptainName { get; set; }
        public bool IsApproved { get; set; }

        public IEnumerable<CommunityTeamMemberModel> Members { get; set; } = new List<CommunityTeamMemberModel>();
    }
}
