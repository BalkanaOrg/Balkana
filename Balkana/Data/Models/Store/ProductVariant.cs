using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models.Store
{
    /// <summary>
    /// Product variant (specific size/color combination)
    /// </summary>
    public class ProductVariant
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        /// <summary>
        /// Variant name (e.g., "Large / Black", "Medium / White")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Complete SKU for this specific variant
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string SKU { get; set; }

        /// <summary>
        /// Variant attributes
        /// </summary>
        [MaxLength(50)]
        public string Size { get; set; }

        [MaxLength(50)]
        public string Color { get; set; }

        /// <summary>
        /// Hex color code for display
        /// </summary>
        [MaxLength(7)]
        public string ColorHex { get; set; }

        /// <summary>
        /// Additional variant info (e.g., "Matte finish", "Glossy")
        /// </summary>
        [MaxLength(100)]
        public string Material { get; set; }

        /// <summary>
        /// Price for this variant (can differ from base price)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Inventory tracking
        /// </summary>
        public int StockQuantity { get; set; }

        /// <summary>
        /// Low stock warning threshold
        /// </summary>
        public int LowStockThreshold { get; set; } = 10;

        /// <summary>
        /// Weight for shipping calculations (in grams)
        /// </summary>
        public int WeightGrams { get; set; }

        /// <summary>
        /// Variant-specific image (optional)
        /// </summary>
        [MaxLength(500)]
        public string ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display order for sorting variants
        /// </summary>
        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

