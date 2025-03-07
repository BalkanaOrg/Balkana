namespace Balkana.Services.Transfers.Models
{
    public class TransfersServiceModel :ITransfersModel
    {
        public int Id { get; init; }

        public string PlayerUsername { get; init; }
        public int PlayerId { get; init; }

        public string TeamFullName { get; init; }
        public int TeamId { get; init; }

        public string GameName { get; init; }

        public DateTime TransferDate { get; init; }

        public string Position { get; init; }
    }
}
