using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models
{
    public class Series
    {
        public int Id { get; set; }

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

        public bool isLive { get; set; } = false;

        public string StreamLink { get; set; }

        public int Match1Id { get; set; }
        public int Match2Id { get; set; }   
        public int Match3Id { get; set; }   
        public int Match4Id { get; set; }   
        public int Match5Id { get; set; }   
        public int Match6Id { get; set; }   
        public int Match7Id { get; set; }   

        [ForeignKey("Match1Id")] public Match Match1 { get; set; }
        [ForeignKey("Match2Id")] public Match Match2 { get; set; }
        [ForeignKey("Match3Id")] public Match Match3 { get; set; }
        [ForeignKey("Match4Id")] public Match Match4 { get; set; }
        [ForeignKey("Match5Id")] public Match Match5 { get; set; }
        [ForeignKey("Match6Id")] public Match Match6 { get; set; }
        [ForeignKey("Match7Id")] public Match Match7 { get; set; }
    }
}
