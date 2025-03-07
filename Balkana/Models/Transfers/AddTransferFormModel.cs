

namespace Balkana.Models.Transfers
{
    using Balkana.Services.Transfers.Models;
    using System.ComponentModel.DataAnnotations;


    using static Data.DataConstants;
    public class AddTransferFormModel
    {
        [Required]
        public string PlayerId { get; init; }

        [Required]
        public string TeamId { get; init; }

        [Required]
        public string GameId { get; init; }

        [Required]
        public string PositionId { get; init; }

        [Required]
        public DateTime TransferDate { get; init; }

        public IEnumerable<TransferPlayerViewModel> TransferPlayers { get; set; }
        public IEnumerable<TransferTeamViewModel> TransferTeams { get; set; }
        public IEnumerable<TransferPositionViewModel> TransferPositions { get; set; }
    }
}
