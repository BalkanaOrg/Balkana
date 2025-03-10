using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models
{
    public class Series
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int TeamAId { get; set; }
        public int TeamBId { get; set; }

        [ForeignKey("TeamAId")]
        public virtual Team TeamA { get; set; }
        
        [ForeignKey("TeamBId")]
        public virtual Team TeamB { get; set; }

        [ForeignKey("GameId")]
        public int GameId { get; set; }
        public virtual Game Game { get; set; }

        [ForeignKey("TournamentId")]
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public DateTime DatePlayed {  get; set; }

        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}
