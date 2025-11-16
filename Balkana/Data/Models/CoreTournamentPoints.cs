namespace Balkana.Data.Models
{
    public class CoreTournamentPoints
    {
        public int Id { get; set; }

        public int CoreId { get; set; }
        public Core Core { get; set; }

        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public int Points { get; set; }   // how many points this core earned
    }
}
