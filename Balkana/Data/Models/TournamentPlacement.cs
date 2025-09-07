namespace Balkana.Data.Models
{
    public class TournamentPlacement
    {
        public int Id { get; set; }

        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public int TeamId { get; set; }   // the branding they played under
        public Team Team { get; set; }

        public int Placement { get; set; } // e.g. 1 = winner, 2 = runner-up
        public int PointsAwarded { get; set; } // total pool points (before distributing to core)
    }
}
