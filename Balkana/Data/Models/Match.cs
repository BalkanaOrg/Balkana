using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    public class Match
    {
        public int Id { get; set; }

        [Required]
        public int MapId { get; set; }
        public csMap Map { get; set; }

        public string VOD { get; set; }

        public int SeriesId { get; set; }
        public Series Series { get; set; }

        public IEnumerable<PlayerStatistic_CS2> Stats_CS2 { get; init; }
    }
}
