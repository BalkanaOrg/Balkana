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

        public int GameId { get; set; }
        public Game Game { get; set; }

        public ICollection<Series> Series { get; set; }
        public ICollection<TrophyTournament> Trophies { get; set; }

    }
}
