using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models.Store
{
    /// <summary>
    /// Product collections (e.g., "Balkana 2025 Winter Collection")
    /// </summary>
    public class Collection
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; }

        /// <summary>
        /// URL-friendly slug
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Slug { get; set; }

        /// <summary>
        /// Banner image URL
        /// </summary>
        [MaxLength(500)]
        public string BannerImageUrl { get; set; }

        /// <summary>
        /// Season/Year (e.g., "Winter 2025", "Spring 2024")
        /// </summary>
        [MaxLength(50)]
        public string Season { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Collection availability dates
        /// </summary>
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }

        /// <summary>
        /// Display order for featured collections
        /// </summary>
        public int DisplayOrder { get; set; }

        public ICollection<ProductCollection> ProductCollections { get; set; } = new List<ProductCollection>();
    }
}

