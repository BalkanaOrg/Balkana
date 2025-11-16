using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models.Store
{
    /// <summary>
    /// Additional product images (gallery)
    /// </summary>
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; }

        [MaxLength(200)]
        public string AltText { get; set; }

        /// <summary>
        /// Display order
        /// </summary>
        public int DisplayOrder { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}

