namespace Balkana.Models.Community
{
    public class CommunityIndexViewModel
    {
        public string SearchTerm { get; set; }
        public List<CommunityUserSearchModel> Users { get; set; } = new();
        public List<CommunityTeamSearchModel> Teams { get; set; } = new();
    }
}
