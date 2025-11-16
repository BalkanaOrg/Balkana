using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models.Store
{
    /// <summary>
    /// Customer order
    /// </summary>
    public class Order
    {
        public int Id { get; set; }

        /// <summary>
        /// Unique order number for customer reference
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; }

        /// <summary>
        /// User ID if registered user, null for guest checkout
        /// </summary>
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        /// <summary>
        /// Guest customer information
        /// </summary>
        [MaxLength(200)]
        public string? GuestEmail { get; set; }

        [MaxLength(100)]
        public string? GuestFirstName { get; set; }

        [MaxLength(100)]
        public string? GuestLastName { get; set; }

        [MaxLength(20)]
        public string? GuestPhone { get; set; }

        /// <summary>
        /// Order totals
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Currency (EUR, BGN, USD)
        /// </summary>
        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Order status
        /// </summary>
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        /// <summary>
        /// Payment information
        /// </summary>
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [MaxLength(200)]
        public string? PaymentTransactionId { get; set; }

        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// Shipping information
        /// </summary>
        public DeliveryProvider DeliveryProvider { get; set; }

        [MaxLength(100)]
        public string? TrackingNumber { get; set; }

        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }

        /// <summary>
        /// Shipping address
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string ShippingAddress { get; set; }

        [Required]
        [MaxLength(100)]
        public string ShippingCity { get; set; }

        [MaxLength(20)]
        public string? ShippingPostalCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string ShippingCountry { get; set; } = "Bulgaria";

        /// <summary>
        /// Billing address (optional, can be same as shipping)
        /// </summary>
        [MaxLength(200)]
        public string? BillingAddress { get; set; }

        [MaxLength(100)]
        public string? BillingCity { get; set; }

        [MaxLength(20)]
        public string? BillingPostalCode { get; set; }

        [MaxLength(100)]
        public string? BillingCountry { get; set; }

        /// <summary>
        /// Delivery office (for Ekont/Speedy office pickup)
        /// </summary>
        [MaxLength(200)]
        public string? DeliveryOfficeAddress { get; set; }

        [MaxLength(100)]
        public string? DeliveryOfficeCode { get; set; }

        /// <summary>
        /// Order notes
        /// </summary>
        [MaxLength(1000)]
        public string? CustomerNotes { get; set; }

        [MaxLength(1000)]
        public string? AdminNotes { get; set; }

        /// <summary>
        /// Timestamps
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        /// <summary>
        /// Order items
        /// </summary>
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Processing = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5,
        Refunded = 6
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Paid = 1,
        Failed = 2,
        Refunded = 3,
        PartiallyRefunded = 4
    }

    public enum PaymentMethod
    {
        CashOnDelivery = 0,
        BankTransfer = 1,
        ePay = 2,           // Bulgarian payment gateway
        Stripe = 3,
        PayPal = 4,
        Card = 5,
        CreditCard = 6,     // Credit/Debit card
        DebitCard = 7       // Debit card
    }

    public enum DeliveryProvider
    {
        Ekont = 0,          // Bulgarian delivery service
        Speedy = 1,         // Bulgarian delivery service
        CourierToAddress = 2,
        OfficePickup = 3,
        InternationalShipping = 4
    }
}

