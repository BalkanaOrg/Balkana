using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    /// <summary>
    /// Staging table for match completion callbacks from Riot.
    /// Admin reviews and manually imports into the database.
    /// </summary>
    public class RiotPendingMatch
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string MatchId { get; set; } = "";

        [MaxLength(100)]
        public string? TournamentCode { get; set; }

        public int? RiotTournamentCodeId { get; set; }
        public RiotTournamentCode? RiotTournamentCode { get; set; }

        [Required]
        public string RawPayload { get; set; } = "{}";

        public RiotPendingMatchStatus Status { get; set; } = RiotPendingMatchStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ImportedAt { get; set; }

        public int? ImportedMatchDbId { get; set; }
        public Match? ImportedMatch { get; set; }

        public int? SeriesId { get; set; }
        public Series? Series { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }
    }

    public enum RiotPendingMatchStatus
    {
        Pending = 0,
        Imported = 1,
        Discarded = 2,
        Failed = 3
    }
}
