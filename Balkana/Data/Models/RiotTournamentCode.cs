using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    /// <summary>
    /// Represents a tournament code generated for a specific match
    /// Players use this code to create custom games that are tracked by Riot API
    /// </summary>
    public class RiotTournamentCode
    {
        public int Id { get; set; }

        /// <summary>
        /// The tournament code string (e.g., "EUW1-XXXX-YYYY-ZZZZ")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Code { get; set; }

        /// <summary>
        /// Link to the Riot Tournament this code belongs to
        /// </summary>
        public int RiotTournamentId { get; set; }
        public RiotTournament RiotTournament { get; set; }

        /// <summary>
        /// Optional metadata about which series/match this code is for
        /// </summary>
        public int? SeriesId { get; set; }
        public Series Series { get; set; }

        /// <summary>
        /// Team A that should use this code
        /// </summary>
        public int? TeamAId { get; set; }
        public Team TeamA { get; set; }

        /// <summary>
        /// Team B that should use this code
        /// </summary>
        public int? TeamBId { get; set; }
        public Team TeamB { get; set; }

        /// <summary>
        /// Description/label for this code (e.g., "Match 1 - Team A vs Team B")
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Map type (e.g., SUMMONERS_RIFT, HOWLING_ABYSS)
        /// </summary>
        [MaxLength(50)]
        public string MapType { get; set; }

        /// <summary>
        /// Pick type (e.g., TOURNAMENT_DRAFT, ALL_RANDOM, etc.)
        /// </summary>
        [MaxLength(50)]
        public string PickType { get; set; }

        /// <summary>
        /// Spectator type (e.g., ALL, NONE, LOBBYONLY)
        /// </summary>
        [MaxLength(50)]
        public string SpectatorType { get; set; }

        /// <summary>
        /// Team size (typically 5)
        /// </summary>
        public int TeamSize { get; set; } = 5;

        /// <summary>
        /// Allowed summoner IDs (PUUIDs) - comma separated
        /// </summary>
        public string AllowedSummonerIds { get; set; }

        /// <summary>
        /// When this code was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether this code has been used (match completed)
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// The match ID generated when this code was used
        /// </summary>
        [MaxLength(100)]
        public string MatchId { get; set; }

        /// <summary>
        /// Link to the imported match (if imported)
        /// </summary>
        public int? MatchDbId { get; set; }
        public Match Match { get; set; }
    }
}

