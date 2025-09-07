namespace Balkana.Data.Models
{
    public class Core
    {
        public int Id { get; set; }

        public string Name { get; set; }   // optional, auto-generate from nicknames

        public ICollection<CorePlayer> Players { get; set; } = new List<CorePlayer>();
        public ICollection<CoreTournamentPoints> TournamentPoints { get; set; } = new List<CoreTournamentPoints>();
    }
}
