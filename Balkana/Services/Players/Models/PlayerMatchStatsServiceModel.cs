using Balkana.Data.Models;

namespace Balkana.Services.Players.Models
{
    public class PlayerMatchStatsServiceModel
    {
        public int MatchId { get; set; }
        public DateTime PlayedAt { get; set; }
        public string Opponent { get; set; }
        public bool IsWinner { get; set; }

        // One of the following gets filled depending on game:
        public PlayerStatistic_CS2? CS2Stats { get; set; }
        public PlayerStatistic_LoL? LoLStats { get; set; }

        // Convenience property to iterate items in Razor
        public List<int> LoLItems => LoLStats != null
            ? new List<int> { LoLStats.Item0, LoLStats.Item1, LoLStats.Item2, LoLStats.Item3, LoLStats.Item4, LoLStats.Item5, LoLStats.Item6 }
            : new List<int>();
    }
}
