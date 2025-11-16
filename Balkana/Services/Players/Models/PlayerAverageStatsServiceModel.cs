namespace Balkana.Services.Players.Models
{
    public class PlayerAverageStatsServiceModel
    {
        public string GameName { get; set; }
        public string Source { get; set; } // "FACEIT", "RIOT", etc.
        
        // CS2/FACEIT Stats
        public double? AverageKills { get; set; }
        public double? AverageDeaths { get; set; }
        public double? AverageAssists { get; set; }
        public double? AverageDamage { get; set; }
        public double? AverageHLTVRating { get; set; }
        public double? AverageHeadshotPercentage { get; set; }
        public double? AverageADR { get; set; }
        public double? AverageKPR { get; set; }
        public double? AverageDPR { get; set; }
        
        // LoL/RIOT Stats
        public double? AverageLoLKills { get; set; }
        public double? AverageLoLDeaths { get; set; }
        public double? AverageLoLAssists { get; set; }
        public double? AverageCreepScore { get; set; }
        public double? AverageVisionScore { get; set; }
        public double? AverageGoldEarned { get; set; }
        
        // Common stats
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate => TotalMatches > 0 ? (double)Wins / TotalMatches * 100 : 0;

        // Map statistics
        public List<PlayerMapStatsServiceModel> MapStats { get; set; } = new();
    }

    public class PlayerMapStatsServiceModel
    {
        public int MapId { get; set; }
        public string MapName { get; set; }
        public string MapDisplayName { get; set; }
        public string PictureURL { get; set; }
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate => MatchesPlayed > 0 ? (double)Wins / MatchesPlayed * 100 : 0;
        public double PickRate { get; set; } // Percentage of total matches played on this map
        public bool IsActiveDuty { get; set; }
        public double AverageHLTVRating { get; set; }
    }
}
