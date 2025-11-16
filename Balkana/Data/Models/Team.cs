namespace Balkana.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.SqlTypes;
    using static DataConstants;
    
    public class Team
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(TeamTagMaxLength)]
        [MinLength(TeamTagMinLength)]
        public string Tag { get; set; }

        [Required]
        [MaxLength(TeamFullNameMaxLength)]
        [MinLength(TeamFullNameMinLength)]
        public string FullName { get; set; }

        [Required]
        public int yearFounded { get; set; }

        [Required]
        public string LogoURL { get; set; }

        [Required]
        public int GameId { get; set; }
        public Game Game { get; set; }

        public ICollection<Series> SeriesAsTeam1 { get; set; } = new List<Series>();
        public ICollection<Series> SeriesAsTeam2 { get; set; } = new List<Series>();
        public ICollection<PlayerTeamTransfer> Transfers { get; set; } = new List<PlayerTeamTransfer>();
        public ICollection<TeamSocials> Socials { get; set; } = new List<TeamSocials>();
        public ICollection<TeamTrophy> TeamTrophies { get; set; } = new List<TeamTrophy>();
        public ICollection<TournamentTeam> TournamentTeams { get; set; } = new List<TournamentTeam>();
        public ICollection<TournamentPlacement> Placements { get; set; } = new List<TournamentPlacement>();

        [NotMapped]
        public IEnumerable<Player> Players => Transfers?.Select(t => t.Player);
    }
}
