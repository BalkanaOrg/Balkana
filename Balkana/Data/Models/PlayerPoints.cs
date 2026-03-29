namespace Balkana.Data.Models
{
    public class PlayerPoints
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }

        public int PointsAwarded { get; set; }

        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }
    }
}
