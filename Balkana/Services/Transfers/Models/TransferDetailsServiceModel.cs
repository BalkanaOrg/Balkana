using Balkana.Services.Teams.Models;

namespace Balkana.Services.Transfers.Models
{
    public class TransferDetailsServiceModel : TransfersServiceModel
    {
        public IEnumerable<TransferPlayersServiceModel> Players;
        public IEnumerable<TransferTeamsServiceModel> Teams;
        public IEnumerable<TeamGameServiceModel> Games;
    }
}
