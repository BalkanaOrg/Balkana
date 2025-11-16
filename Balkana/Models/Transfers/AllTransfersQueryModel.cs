using Balkana.Services.Teams.Models;
using Balkana.Services.Transfers.Models;

namespace Balkana.Models.Transfers
{
    public class AllTransfersQueryModel
    {
        public const int TransfersPerPage = 40;

        public string Player { get; set; }
        public IEnumerable<TransferPlayersServiceModel> ManyPlayers { get; set; }

        public string Game { get; set; }
        public IEnumerable<TeamGameServiceModel> ManyGames { get; set; }

        public string Team { get; set; }
        public IEnumerable<TransferTeamsServiceModel> ManyTeams { get; set; }

        public string Position { get; set; }
        public IEnumerable<TransferPositionsServiceModel> ManyPositions { get; set; }

        public string SearchTerm { get; set; }

        public DateTime? AsOfDate { get; set; } // NEW for “roster at date” queries

        public int CurrentPage { get; set; } = 1;
        public int TotalTransfers { get; set; }

        public IEnumerable<string> Games { get; set; }
        public IEnumerable<string> Positions { get; set; }
        public IEnumerable<string> Players { get; set; }
        public IEnumerable<string> Teams { get; set; }

        public IEnumerable<TransfersServiceModel> Transfers { get; set; }
    }
}
