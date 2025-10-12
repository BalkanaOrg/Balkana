using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Models.Match
{
    public class ManualStatsUploadViewModel
    {
        [Required]
        [Display(Name = "Tournament")]
        public int TournamentId { get; set; }

        [Required]
        [Display(Name = "Series")]
        public int SeriesId { get; set; }

        [Required]
        [Display(Name = "Team A")]
        public int TeamAId { get; set; }

        [Required]
        [Display(Name = "Team B")]
        public int TeamBId { get; set; }

        [Required]
        [Display(Name = "Map")]
        public int MapId { get; set; }

        [Required]
        [Display(Name = "Match Date")]
        public DateTime PlayedAt { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Competition Type")]
        public string CompetitionType { get; set; } = "Manual Upload";

        // Round information
        [Required]
        [Display(Name = "Team A Rounds")]
        public int TeamARounds { get; set; }

        [Required]
        [Display(Name = "Team B Rounds")]
        public int TeamBRounds { get; set; }

        [Required]
        [Display(Name = "Total Rounds")]
        public int TotalRounds { get; set; }

        // Team A Players (5 players)
        [Required]
        [Display(Name = "Team A Player 1")]
        public int TeamAPlayer1Id { get; set; }

        [Required]
        [Display(Name = "Team A Player 2")]
        public int TeamAPlayer2Id { get; set; }

        [Required]
        [Display(Name = "Team A Player 3")]
        public int TeamAPlayer3Id { get; set; }

        [Required]
        [Display(Name = "Team A Player 4")]
        public int TeamAPlayer4Id { get; set; }

        [Required]
        [Display(Name = "Team A Player 5")]
        public int TeamAPlayer5Id { get; set; }

        // Team B Players (5 players)
        [Required]
        [Display(Name = "Team B Player 1")]
        public int TeamBPlayer1Id { get; set; }

        [Required]
        [Display(Name = "Team B Player 2")]
        public int TeamBPlayer2Id { get; set; }

        [Required]
        [Display(Name = "Team B Player 3")]
        public int TeamBPlayer3Id { get; set; }

        [Required]
        [Display(Name = "Team B Player 4")]
        public int TeamBPlayer4Id { get; set; }

        [Required]
        [Display(Name = "Team B Player 5")]
        public int TeamBPlayer5Id { get; set; }

        // Winner
        [Required]
        [Display(Name = "Winning Team")]
        public string WinningTeam { get; set; } = "TeamA"; // "TeamA" or "TeamB"

        // Statistics for each player (Team A)
        public PlayerStatsViewModel TeamAPlayer1Stats { get; set; } = new();
        public PlayerStatsViewModel TeamAPlayer2Stats { get; set; } = new();
        public PlayerStatsViewModel TeamAPlayer3Stats { get; set; } = new();
        public PlayerStatsViewModel TeamAPlayer4Stats { get; set; } = new();
        public PlayerStatsViewModel TeamAPlayer5Stats { get; set; } = new();

        // Statistics for each player (Team B)
        public PlayerStatsViewModel TeamBPlayer1Stats { get; set; } = new();
        public PlayerStatsViewModel TeamBPlayer2Stats { get; set; } = new();
        public PlayerStatsViewModel TeamBPlayer3Stats { get; set; } = new();
        public PlayerStatsViewModel TeamBPlayer4Stats { get; set; } = new();
        public PlayerStatsViewModel TeamBPlayer5Stats { get; set; } = new();

        // Dropdowns
        public List<SelectListItem> Tournaments { get; set; } = new();
        public List<SelectListItem> Maps { get; set; } = new();
        public List<SelectListItem> Players { get; set; } = new();
        public List<SelectListItem> Teams { get; set; } = new();
    }

    public class PlayerStatsViewModel
    {
        [Display(Name = "Kills")]
        public int Kills { get; set; }

        [Display(Name = "Assists")]
        public int Assists { get; set; }

        [Display(Name = "Deaths")]
        public int Deaths { get; set; }

        [Display(Name = "ADR")]
        public int Damage { get; set; }

        [Display(Name = "T Rounds Won")]
        public int TsideRoundsWon { get; set; }

        [Display(Name = "CT Rounds Won")]
        public int CTsideRoundsWon { get; set; }

        [Display(Name = "Rounds Played")]
        public int RoundsPlayed { get; set; }

        [Display(Name = "KAST")]
        public int KAST { get; set; }

        [Display(Name = "Headshot %")]
        public int HSkills { get; set; }

        [Display(Name = "HLTV Rating")]
        public double HLTV1 { get; set; }

        [Display(Name = "Utility Damage")]
        public int UD { get; set; }

        [Display(Name = "First Kills")]
        public int FK { get; set; }

        [Display(Name = "First Deaths")]
        public int FD { get; set; }

        [Display(Name = "1K Rounds")]
        public int _1k { get; set; }

        [Display(Name = "2K Rounds")]
        public int _2k { get; set; }

        [Display(Name = "3K Rounds")]
        public int _3k { get; set; }

        [Display(Name = "4K Rounds")]
        public int _4k { get; set; }

        [Display(Name = "5K Rounds")]
        public int _5k { get; set; }

        [Display(Name = "1v1 Clutches")]
        public int _1v1 { get; set; }

        [Display(Name = "1v2 Clutches")]
        public int _1v2 { get; set; }

        [Display(Name = "1v3 Clutches")]
        public int _1v3 { get; set; }

        [Display(Name = "1v4 Clutches")]
        public int _1v4 { get; set; }

        [Display(Name = "1v5 Clutches")]
        public int _1v5 { get; set; }

        [Display(Name = "AWP Kills")]
        public int SniperKills { get; set; }

        [Display(Name = "Pistol Kills")]
        public int PistolKills { get; set; }

        [Display(Name = "Knife Kills")]
        public int KnifeKills { get; set; }

        [Display(Name = "Flashes")]
        public int Flashes { get; set; }
    }
}
