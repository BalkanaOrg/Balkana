using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    /// <summary>
    /// Default Discord channel (snowflake id) for posting tournament results for a game title.
    /// </summary>
    public class DiscordGameResultChannel
    {
        public int Id { get; set; }

        public int GameId { get; set; }

        /// <summary>Optional navigation; not set on form posts.</summary>
        public Game? Game { get; set; }

        [Required]
        [MaxLength(64)]
        public string DiscordChannelId { get; set; } = "";

        [MaxLength(200)]
        public string? DisplayLabel { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
