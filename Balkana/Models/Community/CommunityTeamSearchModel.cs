namespace Balkana.Models.Community
{
    public class CommunityTeamSearchModel
    {
        public int Id { get; set; }
        public string Tag { get; set; }
        public string FullName { get; set; }
        public string LogoUrl { get; set; }

        public bool IsApproved { get; set; }

        public int PlayersCount { get; set; }
        public int SubstitutesCount { get; set; }
        public int CoachesCount { get; set; }

        public bool IsMember { get; set; }
    }
}
