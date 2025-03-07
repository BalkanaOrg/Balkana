using Balkana.Services.Teams.Models;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Services.Transfers.Models
{
    public class TransferFormModel : ITransfersModel
    {
        [Required]
        [Display(Name = "PlayerId")]
        public int PlayerId { get; init; }

        [Required]
        [Display(Name = "TeamId")]
        public int TeamId {get; init; }

        [Required]
        [Display(Name = "PositionId")]
        public int PositionId { get; init; }

        [Required]
        [Display(Name = "GameId")]
        public int GameId { get; init; }

        [Required]
        public DateTime TransferDate { get; init; }

        public IEnumerable<TransferPlayersServiceModel> TransferPlayers { get; set; }
        public IEnumerable<TransferTeamsServiceModel> TransferTeams { get; set; }
        public IEnumerable<TransferPositionsServiceModel> TransferPositions { get; set; }
        public IEnumerable<TeamGameServiceModel> TransferGames { get; set; }
    }
}
