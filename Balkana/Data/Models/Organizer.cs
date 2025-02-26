namespace Balkana.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using static DataConstants;

    public class Organizer
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(TournamentFullNameMaxLength)]
        public string FullName { get; set; }
        [Required]
        [MaxLength(10)]
        public string Tag { get; set; }
        [Required]
        public string Description { get; set; }

        public IEnumerable<Tournament> Tournaments { get; init; }
    }
}
