using Balkana.Data.Models;

namespace Balkana.Services.Transfers.Models
{
    public class TransfersServiceModel :ITransfersModel
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public string PlayerUsername { get; set; }

        public int TeamId { get; set; }
        public string TeamFullName { get; set; }

        public int GameId { get; set; }
        public string GameName { get; set; }

        public DateTime TransferDate { get; set; }

        public int PositionId { get; set; }
        public string Position { get; set; }
    }
}
