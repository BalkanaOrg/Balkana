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
            int currentPage = 1,
            int teamsPerPage = int.MaxValue
        );
        int Create(
            string teamFullName,
            string teamTag,
            string logoUrl,
            int yearFounded,
            int gameId);
    
        bool Edit(
            int teamId,
            string teamFullName,
            string teamTag,
            string logoUrl,
            int yearFounded,
            int gameId);

        TeamDetailsServiceModel Details(int teamId);

        bool GameExists(int gameId);
        public IEnumerable<string> GetAllGames();
        public IEnumerable<TeamGameServiceModel> AllGames();
        public IEnumerable<TeamStaffServiceModel> AllPlayers(int teamId);
    }
}
