namespace Balkana.Models.Stats
{
    public class PlayerStatsResponseModel
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string PlayerNickname { get; set; }
        public string Provider { get; set; }
        public string PlayerUUID { get; set; }
        
        // Game-specific stats
        public CS2StatsModel? CS2Stats { get; set; }
        public LoLStatsModel? LoLStats { get; set; }
        
        // Metadata
        public int TotalMatches { get; set; }
        public DateTime? FirstMatchDate { get; set; }
        public DateTime? LastMatchDate { get; set; }
        public List<string> TeamsPlayedFor { get; set; } = new();
    }
    
    public class CS2StatsModel
    {
        // Basic stats
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalAssists { get; set; }
        public int TotalDamage { get; set; }
        public int TotalRounds { get; set; }
        
        // Calculated stats
        public double KDRatio { get; set; }
        public double ADR { get; set; }
        public double HLTVRating { get; set; }
        public double KAST { get; set; }
        
        // Advanced stats
        public int Headshots { get; set; }
        public int FirstKills { get; set; }
        public int FirstDeaths { get; set; }
        public int MultiKills { get; set; }
        public int Clutches { get; set; }
        
        // Weapon stats
        public int SniperKills { get; set; }
        public int PistolKills { get; set; }
        public int KnifeKills { get; set; }
        
        // Utility stats
        public int Flashes { get; set; }
        public int UtilityUsage { get; set; }
        
        // Map performance
        public List<MapStatsModel> MapStats { get; set; } = new();
    }
    
    public class LoLStatsModel
    {
        // Basic stats
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalAssists { get; set; }
        public int TotalGoldEarned { get; set; }
        public int TotalCreepScore { get; set; }
        public int TotalVisionScore { get; set; }
        
        // Calculated stats
        public double KDRatio { get; set; }
        public double KPARatio { get; set; }
        public double GoldPerMinute { get; set; }
        public double CSPerMinute { get; set; }
        
        // Damage stats
        public int TotalDamageToChampions { get; set; }
        public int TotalDamageToObjectives { get; set; }
        
        // Champion performance
        public List<ChampionStatsModel> ChampionStats { get; set; } = new();
        public List<LaneStatsModel> LaneStats { get; set; } = new();
    }
    
    public class MapStatsModel
    {
        public int MapId { get; set; }
        public string MapName { get; set; }
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double AverageRating { get; set; }
        public double AverageADR { get; set; }
        public double AverageKDRatio { get; set; }
    }
    
    public class ChampionStatsModel
    {
        public int ChampionId { get; set; }
        public string ChampionName { get; set; }
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double AverageKDRatio { get; set; }
        public double AverageKPARatio { get; set; }
    }
    
    public class LaneStatsModel
    {
        public string Lane { get; set; }
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double AverageKDRatio { get; set; }
    }
}
