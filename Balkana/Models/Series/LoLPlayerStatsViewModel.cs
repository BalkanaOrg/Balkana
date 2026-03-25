using System.Collections.Generic;

namespace Balkana.Models.Series
{
    public class LoLPlayerStatsViewModel
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Team { get; set; } // "Team1" / "Team2"

        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }

        public int CreepScore { get; set; }
        public int VisionScore { get; set; }
        public int TotalDamageToChampions { get; set; }

        public bool IsWinner { get; set; }

        // Icon data (only populated for map-specific requests; omitted in "All Maps")
        public string? ChampionName { get; set; }
        public List<int> ItemIds { get; set; } = new();

        // DataDragon patch-aware version (e.g. "16.6.1"), populated for map-specific requests
        public string? DDragonVersion { get; set; }
    }
}

