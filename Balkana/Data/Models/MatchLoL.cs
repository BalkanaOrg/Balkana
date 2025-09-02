namespace Balkana.Data.Models
{
    public class MatchLoL : Match
    {
        public string GameVersion { get; set; }
        public int MapId { get; set; }
        public string GameMode { get; set; }
    }
}
