namespace Balkana.Data.Models
{
    public class TrophyTournament : Trophy
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }
    }
}
