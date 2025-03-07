using Balkana.Services.Teams.Models;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Services.Transfers.Models
{
    public class TransferFormModel : ITransfersModel
    {
        public int PlayerId { get; init; }

        public int TeamId {get; init; }

        public int PositionId { get; init; }

        public int GameId { get; init; }

        public DateTime TransferDate { get; init; }

        public IEnumerable<TransferPlayersServiceModel>? TransferPlayers { get; set; }
        public IEnumerable<TransferTeamsServiceModel>? TransferTeams { get; set; }
        public IEnumerable<TransferPositionsServiceModel>? TransferPositions { get; set; }
        public IEnumerable<TeamGameServiceModel>? TransferGames { get; set; }
    }
}
