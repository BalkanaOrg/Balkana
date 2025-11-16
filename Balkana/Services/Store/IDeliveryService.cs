using Balkana.Data.Models.Store;

namespace Balkana.Services.Store
{
    public interface IDeliveryService
    {
        /// <summary>
        /// Get list of Ekont offices for a city
        /// </summary>
        Task<List<DeliveryOffice>> GetEkontOfficesAsync(string city);

        /// <summary>
        /// Get list of Speedy offices for a city
        /// </summary>
        Task<List<DeliveryOffice>> GetSpeedyOfficesAsync(string city);

        /// <summary>
        /// Calculate shipping cost
        /// </summary>
        Task<decimal> CalculateShippingCostAsync(DeliveryProvider provider, string city, int weightGrams);

        /// <summary>
        /// Create shipment with delivery provider
        /// </summary>
        Task<ShipmentCreationResult> CreateShipmentAsync(int orderId);

        /// <summary>
        /// Track shipment status
        /// </summary>
        Task<ShipmentTrackingResult> TrackShipmentAsync(string trackingNumber, DeliveryProvider provider);
    }

    public class DeliveryOffice
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }
        public string WorkingHours { get; set; }
    }

    public class ShipmentCreationResult
    {
        public bool Success { get; set; }
        public string TrackingNumber { get; set; }
        public string LabelUrl { get; set; }  // PDF label for printing
        public string ErrorMessage { get; set; }
    }

    public class ShipmentTrackingResult
    {
        public bool Success { get; set; }
        public string TrackingNumber { get; set; }
        public string Status { get; set; }
        public string CurrentLocation { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public List<TrackingEvent> Events { get; set; } = new List<TrackingEvent>();
    }

    public class TrackingEvent
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
    }
}

