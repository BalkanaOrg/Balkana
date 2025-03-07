using Balkana.Models.Transfers;
using Balkana.Services.Teams.Models;
using Balkana.Services.Transfers.Models;

namespace Balkana.Services.Transfers
{
    public interface ITransferService
    {
        public TransferQueryServiceModel All(
            string game = null,
            string searchTerm = null,
            int currentPage = 1,
            int transfersPerPage = int.MaxValue
            );

        int Create(
            int playerId,
            int teamId,
            DateTime date,
            int positionId
            );

        bool Edit(
            int transferId,
            int playerId,
            int teamId,
            DateTime date,
            int positionId
            );

        TransferDetailsServiceModel Details(int teamId);

        bool TeamExists(int teamId);
        bool PlayerExists(int playerId);
        bool GameExists(int gameId);
        bool PositionExists(int positionId);

        public IEnumerable<string> GetAllTeams();
        public IEnumerable<string> GetAllTeamsById(int gameId);
        public IEnumerable<TransferTeamsServiceModel> AllTeams(int gameId);

        public IEnumerable<string> GetAllPlayers();
        public IEnumerable<string> GetAllPlayersById(int gameId);
        public IEnumerable<TransferPlayersServiceModel> AllPlayers(int gameId);

        public IEnumerable<string> GetAllGames();
        public IEnumerable<TeamGameServiceModel> AllGames();
    }
}
