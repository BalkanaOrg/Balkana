using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    /// <summary>
    /// Represents a Riot Tournament created via Tournament API v5
    /// Used for League of Legends custom tournaments
    /// </summary>
    public class RiotTournament
    {
        public int Id { get; set; }

        /// <summary>
        /// The tournament ID returned by Riot API
        /// </summary>
        [Required]
        public int RiotTournamentId { get; set; }

        /// <summary>
        /// Human-readable name for this tournament
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Provider ID from Riot (links to your tournament provider)
        /// </summary>
        [Required]
        public int ProviderId { get; set; }

        /// <summary>
        /// Region where tournament is hosted (e.g., EUW1, NA1, etc.)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Region { get; set; }

        /// <summary>
        /// When this tournament was created in our system
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional: Link to internal Tournament entity
        /// </summary>
        public int? TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        /// <summary>
        /// Generated tournament codes for this tournament
        /// </summary>
        public ICollection<RiotTournamentCode> TournamentCodes { get; set; } = new List<RiotTournamentCode>();
    }
}

