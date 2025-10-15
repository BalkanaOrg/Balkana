using Balkana.Data.Models;

namespace Balkana.Models.Tournaments
{
    public class RiotTournamentDetailsViewModel
    {
        public int Id { get; set; }
        public int RiotTournamentId { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public int ProviderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? TournamentId { get; set; }
        public string TournamentName { get; set; }
        
        public List<RiotTournamentCodeItemViewModel> TournamentCodes { get; set; } = new List<RiotTournamentCodeItemViewModel>();
    }

    public class RiotTournamentCodeItemViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public int? SeriesId { get; set; }
        public string SeriesName { get; set; }
        public int? TeamAId { get; set; }
        public string TeamAName { get; set; }
        public int? TeamBId { get; set; }
        public string TeamBName { get; set; }
        public string MapType { get; set; }
        public string PickType { get; set; }
        public string SpectatorType { get; set; }
        public int TeamSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsUsed { get; set; }
        public string MatchId { get; set; }
        public int? MatchDbId { get; set; }
    }
}

