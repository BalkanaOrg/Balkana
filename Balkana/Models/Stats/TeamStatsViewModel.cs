namespace Balkana.Models.Stats
{
    public class TeamStatsViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TeamTag { get; set; }
        public string GameName { get; set; }
        public string LogoUrl { get; set; }
        
        // Team roster options
        public TeamRosterType RosterType { get; set; } = TeamRosterType.CurrentRoster;
        
        // Statistics
        public List<PlayerStatsResponseModel> PlayerStats { get; set; } = new();
        
        // Team aggregated stats
        public TeamAggregatedStats TeamStats { get; set; }
        
        // Metadata
        public int TotalPlayers { get; set; }
        public DateTime? FirstMatchDate { get; set; }
        public DateTime? LastMatchDate { get; set; }
        public int TotalMatches { get; set; }
    }
    
    public enum TeamRosterType
    {
        CurrentRoster,    // Only Active/Substitute/EmergencySubstitute players
        EveryPlayer      // All players who ever played for the team
    }
    
    public class TeamAggregatedStats
    {
        // CS2 Team Stats
        public CS2TeamStatsModel? CS2Stats { get; set; }
        
        // LoL Team Stats
        public LoLTeamStatsModel? LoLStats { get; set; }
    }
    
    public class CS2TeamStatsModel
    {
        // Basic team stats
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalAssists { get; set; }
        public int TotalDamage { get; set; }
        public int TotalRounds { get; set; }
        
        // Calculated team stats
        public double TeamKDRatio { get; set; }
        public double TeamADR { get; set; }
        public double TeamHLTVRating { get; set; }
        public double TeamKAST { get; set; }
        
        // Team advanced stats
        public int TeamHeadshots { get; set; }
        public int TeamFirstKills { get; set; }
        public int TeamFirstDeaths { get; set; }
        
        // Team multi-kill stats
        public int Team_5k { get; set; }
        public int Team_4k { get; set; }
        public int Team_3k { get; set; }
        public int Team_2k { get; set; }
        public int Team_1k { get; set; }
        
        // Team clutch stats
        public int Team_1v1 { get; set; }
        public int Team_1v2 { get; set; }
        public int Team_1v3 { get; set; }
        public int Team_1v4 { get; set; }
        public int Team_1v5 { get; set; }
        
        // Team weapon stats
        public int TeamSniperKills { get; set; }
        public int TeamPistolKills { get; set; }
        public int TeamKnifeKills { get; set; }
        public int TeamWallbangKills { get; set; }
        public int TeamCollateralKills { get; set; }
        public int TeamNoScopeKills { get; set; }
        
        // Team utility stats
        public int TeamFlashes { get; set; }
        public int TeamUtilityUsage { get; set; }
    }
    
    public class LoLTeamStatsModel
    {
        // Basic team stats
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalAssists { get; set; }
        public int TotalGoldEarned { get; set; }
        public int TotalCreepScore { get; set; }
        public int TotalVisionScore { get; set; }
        
        // Calculated team stats
        public double TeamKDRatio { get; set; }
        public double TeamKPARatio { get; set; }
        public double TeamGoldPerMinute { get; set; }
        public double TeamCSPerMinute { get; set; }
        
        // Team damage stats
        public int TeamDamageToChampions { get; set; }
        public int TeamDamageToObjectives { get; set; }
    }
}
