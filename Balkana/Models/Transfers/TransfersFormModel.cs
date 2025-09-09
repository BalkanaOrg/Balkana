using Balkana.Services.Teams.Models;
using Balkana.Services.Transfers.Models;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Transfers
{
    public class TransferFormModel
    {
        [Required]
        public int PlayerId { get; set; }

        [Required]
        public int TeamId { get; set; }

        [Required]
        public int PositionId { get; set; }

        [Required]
        public int GameId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime TransferDate { get; set; }

        // dropdown lists
        public IEnumerable<TransferPlayersServiceModel> TransferPlayers { get; set; } = new List<TransferPlayersServiceModel>();
        public IEnumerable<TransferTeamsServiceModel> TransferTeams { get; set; } = new List<TransferTeamsServiceModel>();
        public IEnumerable<TransferPositionsServiceModel> TransferPositions { get; set; } = new List<TransferPositionsServiceModel>();
        public IEnumerable<TeamGameServiceModel> TransferGames { get; set; } = new List<TeamGameServiceModel>();
    }
}
