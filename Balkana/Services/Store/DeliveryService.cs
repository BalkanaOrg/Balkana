using Balkana.Data;
using Balkana.Data.Models.Store;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Store
{
    /// <summary>
    /// Delivery service with Ekont and Speedy integration
    /// </summary>
    public class DeliveryService : IDeliveryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public DeliveryService(ApplicationDbContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<List<DeliveryOffice>> GetEkontOfficesAsync(string city)
        {
            // TODO: Integrate with Ekont API
            // https://www.ekont.com/en/api
            
            // Mock data for now
            var offices = new List<DeliveryOffice>
            {
                new DeliveryOffice
                {
                    Code = "EKONT-SOF-001",
                    Name = "Ekont Sofia - Center",
                    Address = "бул. Витоша 1",
                    City = "Sofia",
                    PostalCode = "1000",
                    Phone = "+359 700 10 171",
                    WorkingHours = "Mon-Fri: 9:00-18:00, Sat: 10:00-14:00"
                }
            };

            return await Task.FromResult(offices.Where(o => o.City.Equals(city, StringComparison.OrdinalIgnoreCase)).ToList());
        }

        public async Task<List<DeliveryOffice>> GetSpeedyOfficesAsync(string city)
        {
            // TODO: Integrate with Speedy API
            // https://www.speedy.bg/en/web-services
            
            // Mock data for now
            var offices = new List<DeliveryOffice>
            {
                new DeliveryOffice
                {
                    Code = "SPEEDY-SOF-001",
                    Name = "Speedy Sofia - NDK",
                    Address = "бул. България 1",
                    City = "Sofia",
                    PostalCode = "1000",
                    Phone = "+359 700 11 111",
                    WorkingHours = "Mon-Fri: 8:30-18:30, Sat: 9:00-13:00"
                }
            };

            return await Task.FromResult(offices.Where(o => o.City.Equals(city, StringComparison.OrdinalIgnoreCase)).ToList());
        }

        public async Task<decimal> CalculateShippingCostAsync(DeliveryProvider provider, string city, int weightGrams)
        {
            // TODO: Query actual API for shipping rates
            // For now, use fixed rates
            
            return provider switch
            {
                DeliveryProvider.Ekont => weightGrams > 1000 ? 6.00m : 5.00m,
                DeliveryProvider.Speedy => weightGrams > 1000 ? 6.50m : 5.50m,
                DeliveryProvider.CourierToAddress => 8.00m + (weightGrams > 2000 ? 2.00m : 0),
                DeliveryProvider.OfficePickup => 0.00m,
                DeliveryProvider.InternationalShipping => 25.00m + (weightGrams / 1000 * 5.00m),
                _ => 5.00m
            };
        }

        public async Task<ShipmentCreationResult> CreateShipmentAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new Exception("Order not found");

            // TODO: Integrate with Ekont/Speedy API to create shipment
            // For now, generate mock tracking number

            var trackingPrefix = order.DeliveryProvider switch
            {
                DeliveryProvider.Ekont => "EKONT",
                DeliveryProvider.Speedy => "SPEEDY",
                _ => "TRACK"
            };

            var trackingNumber = $"{trackingPrefix}-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(10000, 99999)}";

            return new ShipmentCreationResult
            {
                Success = true,
                TrackingNumber = trackingNumber,
                LabelUrl = null // Would be PDF URL from API
            };
        }

        public async Task<ShipmentTrackingResult> TrackShipmentAsync(string trackingNumber, DeliveryProvider provider)
        {
            // TODO: Integrate with Ekont/Speedy tracking API
            
            return new ShipmentTrackingResult
            {
                Success = true,
                TrackingNumber = trackingNumber,
                Status = "In Transit",
                CurrentLocation = "Sofia Distribution Center",
                EstimatedDelivery = DateTime.UtcNow.AddDays(2),
                Events = new List<TrackingEvent>
                {
                    new TrackingEvent
                    {
                        Timestamp = DateTime.UtcNow.AddHours(-2),
                        Status = "Picked Up",
                        Location = "Balkana Office",
                        Description = "Package picked up from sender"
                    },
                    new TrackingEvent
                    {
                        Timestamp = DateTime.UtcNow.AddHours(-1),
                        Status = "In Transit",
                        Location = "Sofia Distribution Center",
                        Description = "Package arrived at distribution center"
                    }
                }
            };
        }
    }
}

