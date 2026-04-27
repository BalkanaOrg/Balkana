namespace Balkana.Data.Models
{
    public class MatchLoL : Match
    {
        public string GameVersion { get; set; }
        public int MapId { get; set; }
        public string GameMode { get; set; }

        /// <summary>Riot <c>info.gameDuration</c> in seconds; used for DPM and per-minute stats.</summary>
        public int GameDurationSeconds { get; set; }
    }
}
