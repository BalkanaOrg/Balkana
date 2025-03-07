using Balkana.Services.Teams.Models;

namespace Balkana.Services.Transfers.Models
{
    public class TransferQueryServiceModel
    {
        public int CurrentPage { get; init; }

        public int TransfersPerPage { get; init; }

        public int TotalTransfers { get; init; }

        public IEnumerable<TransfersServiceModel> Transfers { get; init; }
    }
}
