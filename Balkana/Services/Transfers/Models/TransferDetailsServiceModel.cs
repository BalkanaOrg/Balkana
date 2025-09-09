using Balkana.Services.Teams.Models;

namespace Balkana.Services.Transfers.Models
{
    public class TransferDetailsServiceModel : TransfersServiceModel
    {
        public IEnumerable<TransferPlayersServiceModel> Players { get; set; }
        public IEnumerable<TransferTeamsServiceModel> Teams { get; set; }
        public IEnumerable<TeamGameServiceModel> Games { get; set; }
        public IEnumerable<TransferPositionsServiceModel> Positions { get; set; }
    }
}
