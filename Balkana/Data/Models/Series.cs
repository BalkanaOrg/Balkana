using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models
{
    public class Series
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public int TeamAId { get; set; }
        public int TeamBId { get; set; }
        public Team TeamA { get; set; }
        public Team TeamB { get; set; }

        public DateTime DatePlayed { get; set; }

        public bool isFinished { get; set; } = false;

        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}
