namespace Balkana.Models.Series
{
    public class PlayerStatsViewModel
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Team { get; set; }
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalAssists { get; set; }
        public int TotalDamage { get; set; }
        public int TotalRounds { get; set; }
        public int MapsPlayed { get; set; }
        public bool IsWinner { get; set; }
        public double HLTVRating { get; set; }
    }
}
