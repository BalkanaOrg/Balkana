using Balkana.Data.Models;
using Balkana.Services.Teams.Models;

namespace Balkana.Services.Transfers.Models
{
    public class TransferDetailsServiceModel
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public string PlayerUsername { get; set; }

        public int? TeamId { get; set; }
        public string TeamFullName { get; set; }

        public int GameId { get; set; }
        public string GameName { get; set; }

        public int PositionId { get; set; }
        public string Position { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public PlayerTeamStatus Status { get; set; }

        public IEnumerable<TransferPlayersServiceModel> Players { get; set; } = new List<TransferPlayersServiceModel>();
        public IEnumerable<TransferTeamsServiceModel> Teams { get; set; } = new List<TransferTeamsServiceModel>();
        public IEnumerable<TeamGameServiceModel> Games { get; set; } = new List<TeamGameServiceModel>();
    }
}
