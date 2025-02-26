using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    using static DataConstants;

    public class Nationality
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(40)]
        public string Name { get; set; }

        [Required]
        public string FlagURL { get; set; } //or emoji, idk

        public IEnumerable<Player> Players { get; set; }
    }
}
