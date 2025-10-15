using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models.Store
{
    /// <summary>
    /// Inventory change log for tracking stock movements
    /// </summary>
    public class InventoryLog
    {
        public int Id { get; set; }

        [Required]
        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        /// <summary>
        /// Quantity change (positive for additions, negative for sales)
        /// </summary>
        public int QuantityChange { get; set; }

        /// <summary>
        /// Stock level after this change
        /// </summary>
        public int StockAfter { get; set; }

        /// <summary>
        /// Change type
        /// </summary>
        public InventoryChangeType ChangeType { get; set; }

        /// <summary>
        /// Related order (if this was a sale)
        /// </summary>
        public int? OrderId { get; set; }
        public Order Order { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        /// <summary>
        /// Admin who made the change (optional for system-generated changes)
        /// </summary>
        public string? ChangedByUserId { get; set; }
        public ApplicationUser ChangedByUser { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }

    public enum InventoryChangeType
    {
        InitialStock = 0,
        Restock = 1,
        Sale = 2,
        Return = 3,
        Damaged = 4,
        Adjustment = 5,
        Reserved = 6,    // For pending orders
        Released = 7     // When order is cancelled
    }
}

