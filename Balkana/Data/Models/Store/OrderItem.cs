using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models.Store
{
    /// <summary>
    /// Individual item in an order
    /// </summary>
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        /// <summary>
        /// Snapshot of product info at time of order
        /// (in case product changes later)
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; }

        [Required]
        [MaxLength(200)]
        public string VariantName { get; set; }

        [MaxLength(50)]
        public string SKU { get; set; }

        /// <summary>
        /// Price at time of purchase
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        /// <summary>
        /// Total = UnitPrice * Quantity
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Discount applied to this item
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        /// <summary>
        /// Product image snapshot
        /// </summary>
        [MaxLength(500)]
        public string ProductImageUrl { get; set; }
    }
}

