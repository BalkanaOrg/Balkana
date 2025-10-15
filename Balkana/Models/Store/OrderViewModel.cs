using Balkana.Data.Models.Store;

namespace Balkana.Models.Store
{
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        
        // Customer
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public bool IsGuestOrder { get; set; }
        
        // Items
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
        
        // Totals
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; }
        
        // Status
        public OrderStatus Status { get; set; }
        public string StatusDisplay { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentStatusDisplay { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodDisplay { get; set; }
        public string PaymentTransactionId { get; set; }
        
        // Shipping
        public DeliveryProvider DeliveryProvider { get; set; }
        public string DeliveryProviderDisplay { get; set; }
        public string TrackingNumber { get; set; }
        public string ShippingAddress { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingPostalCode { get; set; }
        public string ShippingCountry { get; set; }
        public string DeliveryOfficeAddress { get; set; }
        
        // Dates
        public DateTime CreatedAt { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? CancelledAt { get; set; }
        
        // Notes
        public string CustomerNotes { get; set; }
        public string AdminNotes { get; set; }
        public string CancellationReason { get; set; }
    }

    public class OrderItemViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string VariantName { get; set; }
        public string SKU { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string ProductImageUrl { get; set; }
        public int ProductId { get; set; }
        public string ProductSlug { get; set; }
    }

    public class OrderListViewModel
    {
        public List<OrderSummaryViewModel> Orders { get; set; } = new List<OrderSummaryViewModel>();
        
        // Filters
        public OrderStatus? StatusFilter { get; set; }
        public PaymentStatus? PaymentStatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string SearchTerm { get; set; }
        
        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalOrders { get; set; }
    }

    public class OrderSummaryViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
    }
}

