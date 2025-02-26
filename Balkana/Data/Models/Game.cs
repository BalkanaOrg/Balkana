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

        public IEnumerable<Team> Teams { get; init; } = new List<Team>();
    }
}
