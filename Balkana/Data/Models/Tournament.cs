namespace Balkana.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using static DataConstants;

    public class Tournament
    {
        public int Id { get; set; }

        [Required] 
        [MaxLength(TournamentFullNameMaxLength)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(TournamentShortNameMaxLength)]
        public string ShortName { get; set; }
        [Required]
        public int OrganizerId { get; set; }
        public Organizer Organizer { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal PrizePool { get; set; } = 0;

        public string BannerUrl { get; set; }
        public EliminationType Elimination { get; set; }  // single or double

        public int GameId { get; set; }
        public Game Game { get; set; }

        public ICollection<Series> Series { get; set; } = new List<Series>();
        public ICollection<TrophyTournament> Trophies { get; set; } = new List<TrophyTournament>();
        public ICollection<TournamentSocials> Socials { get; set; } = new List<TournamentSocials>();
        public ICollection<TournamentTeam> TournamentTeams { get; set; } = new List<TournamentTeam>();
        public ICollection<TournamentPlacement> Placements { get; set; } = new List<TournamentPlacement>();

    }
}
