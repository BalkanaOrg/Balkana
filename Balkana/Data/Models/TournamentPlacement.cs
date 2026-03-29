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
        /// <summary>Player pool after emergency-substitute penalty (split across players).</summary>
        public int PointsAwarded { get; set; }
        /// <summary>20% of <see cref="PointsAwarded"/> awarded to the team organisation.</summary>
        public int OrganisationPointsAwarded { get; set; }
    }
}

