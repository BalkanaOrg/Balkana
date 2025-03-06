

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

        [Required]
        [MaxLength(NameMaxLength)]
        [MinLength(NameMinLength)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(NameMaxLength)]
        [MinLength(NameMinLength)]
        public string LastName { get; set; }

        [Required]
        public int NationalityId { get; set; }
        [ForeignKey("NationalityId")]
        public Nationality Nationality { get; set; }

        public ICollection<PlayerPicture> PlayerPictures { get; init; }
        public ICollection<PlayerStatistic_CS2> Stats_CS { get; init; }
        public ICollection<PlayerTeamTransfer> Transfers { get; set; }
    }
}
