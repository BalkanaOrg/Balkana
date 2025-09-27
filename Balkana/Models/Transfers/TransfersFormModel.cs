using Balkana.Data.Models;
using Balkana.Services.Teams.Models;
using Balkana.Services.Transfers.Models;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Transfers
{
    public class TransferFormModel
    {
        public int PlayerId { get; set; }
        public int? TeamId { get; set; }
        public int GameId { get; set; }
        public int PositionId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public PlayerTeamStatus Status { get; set; }

        // ✅ Use service models instead of Data.Models
        public IEnumerable<TeamGameServiceModel> TransferGames { get; set; } = new List<TeamGameServiceModel>();
        public IEnumerable<TransferTeamsServiceModel> TransferTeams { get; set; } = new List<TransferTeamsServiceModel>();
        public IEnumerable<TransferPlayersServiceModel> TransferPlayers { get; set; } = new List<TransferPlayersServiceModel>();
        public IEnumerable<TransferPositionsServiceModel> TransferPositions { get; set; } = new List<TransferPositionsServiceModel>();
    }

}
