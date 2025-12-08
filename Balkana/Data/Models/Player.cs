

namespace Balkana.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Diagnostics.CodeAnalysis;
    using static DataConstants;

    public class Player
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(NameMaxLength)]
        [MinLength(NameMinLength)]
        public string Nickname { get; set; }

        [MaxLength(NameMaxLength)]
        public string? FirstName { get; set; }

        [MaxLength(NameMaxLength)]
        public string? LastName { get; set; }

        [Required]
        public int NationalityId { get; set; }
        [ForeignKey("NationalityId")]
        public Nationality Nationality { get; set; }

        public DateTime? BirthDate { get; set; } = DateTime.Now;

        public decimal PrizePoolWon { get; set; } = 0;

        public ICollection<PlayerPicture> PlayerPictures { get; set; } = new List<PlayerPicture>();
        public ICollection<PlayerStatistic_CS2> Stats_CS { get; set; } = new List<PlayerStatistic_CS2>();
        public ICollection<PlayerTeamTransfer> Transfers { get; set; } = new List<PlayerTeamTransfer>();
        public ICollection<GameProfile> GameProfiles { get; set; } = new List<GameProfile>();
        public ICollection<PlayerSocials> Socials { get; set; } = new List<PlayerSocials>();
        public ICollection<PlayerTrophy> PlayerTrophies { get; set; } = new List<PlayerTrophy>();
    }
}
