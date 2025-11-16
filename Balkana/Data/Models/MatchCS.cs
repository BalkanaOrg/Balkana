namespace Balkana.Data.Models
{
    public class MatchCS : Match
    {
        public int? MapId { get; set; }
        public GameMap Map { get; set; }
        public string CompetitionType { get; set; }
        
        // Round information specific to CS2 matches
        public int? TeamARounds { get; set; }
        public int? TeamBRounds { get; set; }
        public int? TotalRounds { get; set; }
    }
}
