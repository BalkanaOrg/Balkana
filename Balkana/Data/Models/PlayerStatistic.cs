using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    public abstract class PlayerStatistic
    {
        public int Id { get; set; }

        [Required]
        public int MatchId { get; set; }
        public Match Match { get; set; }

        [Required]
        public string PlayerUUID { get; set; } // For external source matching

        [Required]
        public string Source { get; set; } // "RIOT", "FACEIT", etc.

        public bool IsWinner { get; set; }
        public string? Team { get; set; } // could be "Blue"/"Red" or "T/CT"
    }
}
