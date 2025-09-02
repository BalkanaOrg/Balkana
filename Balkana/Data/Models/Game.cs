using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    public class Game
    {
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string ShortName { get; set; }
        [Required]
        public string IconURL { get; set; }

        public ICollection<Team> Teams { get; init; } = new List<Team>();
        public ICollection<TeamPosition> Positions { get; init; } = new List<TeamPosition>();
        public ICollection<Tournament> Tournaments{ get; init; } = new List<Tournament>();
    }
}
