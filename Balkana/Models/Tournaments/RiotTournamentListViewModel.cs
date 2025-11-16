using Balkana.Data.Models;

namespace Balkana.Models.Tournaments
{
    public class RiotTournamentListViewModel
    {
        public List<RiotTournamentItemViewModel> Tournaments { get; set; } = new List<RiotTournamentItemViewModel>();
    }

    public class RiotTournamentItemViewModel
    {
        public int Id { get; set; }
        public int RiotTournamentId { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public int ProviderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? TournamentId { get; set; }
        public string TournamentName { get; set; }
        public int TotalCodes { get; set; }
        public int UsedCodes { get; set; }
        public int UnusedCodes { get; set; }
    }
}

