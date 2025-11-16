using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models.Store
{
    /// <summary>
    /// Product categories (Clothing, Accessories, Stationery, etc.)
    /// </summary>
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// URL-friendly slug for category (e.g., "t-shirts")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Slug { get; set; }

        /// <summary>
        /// Display order for sorting
        /// </summary>
        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Parent category for nested categories (optional)
        /// </summary>
        public int? ParentCategoryId { get; set; }
        public ProductCategory ParentCategory { get; set; }

        public ICollection<ProductCategory> SubCategories { get; set; } = new List<ProductCategory>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

