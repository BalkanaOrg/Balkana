using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    public class GamblingSession
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string GameType { get; set; }

        [Required]
        public decimal BetAmount { get; set; }

        [Required]
        public decimal WinAmount { get; set; }

        [Required]
        public DateTime PlayedAt { get; set; }

        [MaxLength(200)]
        public string Result { get; set; }

        public bool IsWin { get; set; }

        // Navigation property
        public ApplicationUser User { get; set; }
    }

    public class GamblingLeaderboard
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public decimal TotalWinnings { get; set; }

        [Required]
        public int TotalSessions { get; set; }

        [Required]
        public decimal WinRate { get; set; }

        [Required]
        public decimal BiggestWin { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }

        // Navigation property
        public ApplicationUser User { get; set; }
    }
}
