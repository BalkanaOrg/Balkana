namespace Balkana.Services.Teams.Models
{
    public class TeamDetailsServiceModel : TeamServiceModel
    {
        public string Description { get; set; }
        public int GameId { get; init; }
        public string GameName { get; set; }
        public string GameShortName { get; set; }
        
        // Current and historical rosters
        public IEnumerable<TeamRosterServiceModel> CurrentRoster { get; set; } = new List<TeamRosterServiceModel>();
        public IEnumerable<TeamRosterServiceModel> HistoricalRosters { get; set; } = new List<TeamRosterServiceModel>();
        
        // Tournament participation
        public IEnumerable<TeamTournamentServiceModel> Tournaments { get; set; } = new List<TeamTournamentServiceModel>();
        
        // Team trophies
        public IEnumerable<TeamTrophyServiceModel> Trophies { get; set; } = new List<TeamTrophyServiceModel>();
        
        // Match history
        public IEnumerable<TeamMatchServiceModel> RecentMatches { get; set; } = new List<TeamMatchServiceModel>();
        
        // Statistics
        public TeamStatisticsServiceModel CurrentRosterStats { get; set; } = new TeamStatisticsServiceModel();
        public TeamStatisticsServiceModel AllTimeStats { get; set; } = new TeamStatisticsServiceModel();
        
        // Legacy property for backward compatibility
        public IEnumerable<TeamStaffServiceModel> Players { get; set; } = new List<TeamStaffServiceModel>();
    }
}
