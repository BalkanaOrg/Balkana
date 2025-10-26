using Balkana.Models.Stats;

namespace Balkana.Services.Stats
{
    public interface IStatsService
    {
        Task<List<PlayerStatsResponseModel>> GetPlayerStatsAsync(StatsRequestModel request);
        Task<List<PlayerStatsResponseModel>> GetTeamStatsAsync(StatsRequestModel request, TeamRosterType rosterType = TeamRosterType.CurrentRoster);
        Task<List<PlayerStatsResponseModel>> GetSeriesStatsAsync(StatsRequestModel request);
        Task<List<PlayerStatsResponseModel>> GetTournamentStatsAsync(StatsRequestModel request);
        Task<TeamAggregatedStats> GetTeamAggregatedStatsAsync(StatsRequestModel request, TeamRosterType rosterType = TeamRosterType.CurrentRoster);
    }
}
