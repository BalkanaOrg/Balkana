namespace Balkana.Models.BalkanaAwards
{
    public class BalkanaAwardsVotingPageViewModel
    {
        public int Year { get; set; }
        public DateTime? VotingOpensAt { get; set; }
        public DateTime? VotingClosesAt { get; set; }
        public List<BalkanaAwardsCategoryViewModel> Categories { get; set; } = new();
    }

    public class BalkanaAwardsCategoryViewModel
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string TargetType { get; set; }
        public bool IsCommunityVoted { get; set; }
        public bool IsRanked { get; set; }
        public int MaxRanks { get; set; }
    }
}

