namespace Balkana.Data.Models
{
    public class BalkanaAwardCategory
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string TargetType { get; set; } // Player|Team|Tournament|User
        public bool IsCommunityVoted { get; set; }
        public bool IsRanked { get; set; }
        public int MaxRanks { get; set; } = 1;
        public int SortOrder { get; set; } = 0;

        public ICollection<BalkanaAwardEligibilityPlayer> EligiblePlayers { get; set; } = new List<BalkanaAwardEligibilityPlayer>();
        public ICollection<UserVoting> Votes { get; set; } = new List<UserVoting>();
        public ICollection<BalkanaAwardResult> Results { get; set; } = new List<BalkanaAwardResult>();
    }
}

