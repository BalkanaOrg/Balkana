namespace Balkana.Data.Models
{
    using System.ComponentModel.DataAnnotations;
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

        public ICollection<Series> SeriesAsTeam1 { get; set; }
        public ICollection<Series> SeriesAsTeam2 { get; set; }
        public ICollection<PlayerTeamTransfer> Transfers { get; set; }
    }
}
