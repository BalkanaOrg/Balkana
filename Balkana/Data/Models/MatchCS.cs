namespace Balkana.Data.Models
{
    public class MatchCS : Match
    {
        public int? MapId { get; set; }
        public GameMap Map { get; set; }
        public string CompetitionType { get; set; }
    }
}
