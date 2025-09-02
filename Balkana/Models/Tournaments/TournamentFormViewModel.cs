using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Tournaments
{
    public class TournamentFormViewModel
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string FullName { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string ShortName { get; set; }

        [Required]
        public int OrganizerId { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        public int GameId { get; set; }
    }
}
