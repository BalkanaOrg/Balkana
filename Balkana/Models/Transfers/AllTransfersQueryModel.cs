using Balkana.Services.Teams.Models;

namespace Balkana.Models.Transfers
{
    public class AllTransfersQueryModel
    {
        public const int TransfersPerpage = 40;

        public string Game { get; set; }
        public IEnumerable<TeamGameServiceModel> ManyGames { get; set; }

        public string SearchTerm { get; set; }

        public int CurrentPage { get; set; } = 1;

        public int TotalTransfers { get; set; }

        public IEnumerable<string> Games { get; set; }
        public IEnumerable<TransfersServiceModel> Transfers { get; set; }
    }
}
