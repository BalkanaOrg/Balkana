using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models.Store
{
    /// <summary>
    /// Base product (e.g., "Balkana Logo T-Shirt")
    /// </summary>
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; }

        /// <summary>
        /// URL-friendly slug
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Slug { get; set; }

        /// <summary>
        /// SKU (Stock Keeping Unit) prefix for this product
        /// </summary>
        [MaxLength(50)]
        public string SKU { get; set; }

        /// <summary>
        /// Base price (actual price comes from variants)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        /// <summary>
        /// Category
        /// </summary>
        [Required]
        public int CategoryId { get; set; }
        public ProductCategory Category { get; set; }

        /// <summary>
        /// Main product image
        /// </summary>
        [MaxLength(500)]
        public string MainImageUrl { get; set; }

        /// <summary>
        /// Product status
        /// </summary>
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }

        /// <summary>
        /// Optional team/player association for team merchandise
        /// </summary>
        public int? TeamId { get; set; }
        public Team Team { get; set; }

        public int? PlayerId { get; set; }
        public Player Player { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Product variants (sizes, colors, etc.)
        /// </summary>
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        /// <summary>
        /// Additional product images
        /// </summary>
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        /// <summary>
        /// Collections this product belongs to
        /// </summary>
        public ICollection<ProductCollection> ProductCollections { get; set; } = new List<ProductCollection>();
    }
}

