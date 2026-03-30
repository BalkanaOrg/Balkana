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

        /// <summary>Calendar year (UTC) for circuit points in the header.</summary>
        public int CircuitYear { get; set; }

        /// <summary>Sum of <see cref="PlayerPoints"/> for the current active roster in <see cref="CircuitYear"/>.</summary>
        public int CircuitYearRosterPlayerPoints { get; set; }

        /// <summary>Sum of organisation placement points for this team in <see cref="CircuitYear"/>.</summary>
        public int CircuitYearOrganisationPoints { get; set; }

        public int CircuitYearPointsTotal => CircuitYearRosterPlayerPoints + CircuitYearOrganisationPoints;
        
        // Legacy property for backward compatibility
        public IEnumerable<TeamStaffServiceModel> Players { get; set; } = new List<TeamStaffServiceModel>();
    }
}
