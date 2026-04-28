using Balkana.Services.Teams.Models;
using Microsoft.VisualBasic;
using Balkana.Models;

namespace Balkana.Services.Teams
{
    public interface ITeamService
    {
        public TeamQueryServiceModel All(
            string game = null,
            string searchTerm = null,
            int? year = null,
            int currentPage = 1,
            int teamsPerPage = int.MaxValue
        );
        int Create(
            string teamFullName,
            string teamTag,
            string logoUrl,
            int yearFounded,
            int gameId);
    
        bool Update(
            int teamId,
            string teamFullName,
            string teamTag,
            string logoUrl,
            int yearFounded,
            int gameId);

        TeamDetailsServiceModel Details(int teamId);

        bool GameExists(int gameId);
        public IEnumerable<string> GetAllGames();
        public IEnumerable<int> GetAvailableYears();
        public int AbsoluteNumberOfTeams();
        public IEnumerable<TeamGameServiceModel> AllGames();
        public IEnumerable<TeamStaffServiceModel> AllPlayers(int teamId);

        /// <summary>
        /// Teams in the circuit year for the given game, ordered by total circuit points (desc), then team id.
        /// LoL: team placement points + active roster ordered by lane PositionId 9–13.
        /// CS: active roster player points + organisation points, with per-player year totals.
        /// </summary>
        IReadOnlyList<CircuitStandingTeamDto> GetCircuitStandings(string gameFullName, int circuitYear);
    }
}
