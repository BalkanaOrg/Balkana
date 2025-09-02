using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    public abstract class Match
    {
        public int Id { get; set; }

        [Required] public string ExternalMatchId { get; set; } // Riot or FACEIT ID
        [Required] public string Source { get; set; } // "RIOT" | "FACEIT"

        public int SeriesId { get; set; }
        public Series Series { get; set; }

        public DateTime PlayedAt { get; set; }
        public bool IsCompleted { get; set; }

        // Explicit mapping for clarity
        public int? TeamAId { get; set; }
        public Team TeamA { get; set; }
        public int? TeamBId { get; set; }
        public Team TeamB { get; set; }

        // Track which external slot = TeamA/TeamB
        public string TeamASourceSlot { get; set; } // "Blue" / "Team1"
        public string TeamBSourceSlot { get; set; } // "Red" / "Team2"

        public ICollection<PlayerStatistic> PlayerStats { get; set; } = new List<PlayerStatistic>();
    }
}
