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
        bool TransferExists(int id);

        public IEnumerable<string> GetAllTeams();
        public IEnumerable<string> GetAllTeams(int gameId);
        public IEnumerable<TransferTeamsServiceModel> AllTeams();

        public IEnumerable<string> GetAllPlayers();
        public IEnumerable<string> GetAllPlayers(int gameId);
        public IEnumerable<TransferPlayersServiceModel> AllPlayers();

        public IEnumerable<int> GetAllTransfers();
        public IEnumerable<int> GetAllTransfers(int gameId);
        public IEnumerable<TransferPositionsServiceModel> AllTransfers();

        public IEnumerable<string> GetAllPositions();
        public IEnumerable<string> GetAllPositions(int gameId);
        public IEnumerable<TransferPositionsServiceModel> AllPositions();

        public IEnumerable<string> GetAllGames();
        public IEnumerable<TeamGameServiceModel> AllGames();
    }
}
