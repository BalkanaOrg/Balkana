using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Models.Store
{
    public class AdminProductFormViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Product Name")]
        [StringLength(200)]
        public string Name { get; set; }
        
        [Required]
        [Display(Name = "Description")]
        [StringLength(2000)]
        public string Description { get; set; }
        
        [Required]
        [Display(Name = "URL Slug")]
        [StringLength(200)]
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens")]
        public string Slug { get; set; }
        
        [Display(Name = "SKU Prefix")]
        [StringLength(50)]
        public string SKU { get; set; }
        
        [Required]
        [Display(Name = "Base Price (BGN)")]
        [Range(0.01, 999999)]
        public decimal BasePrice { get; set; }
        
        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        
        [Display(Name = "Main Image URL")]
        [StringLength(500)]
        public string MainImageUrl { get; set; }
        
        [Display(Name = "Team (Optional)")]
        public int? TeamId { get; set; }
        
        [Display(Name = "Player (Optional)")]
        public int? PlayerId { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; }
        
        // Dropdowns
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Teams { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Players { get; set; } = new List<SelectListItem>();
    }

    public class AdminProductVariantFormViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }  // Display only, not required for binding
        
        [Required]
        [Display(Name = "Variant Name")]
        [StringLength(200)]
        public string Name { get; set; }
        
        [Required]
        [Display(Name = "SKU")]
        [StringLength(50)]
        public string SKU { get; set; }
        
        [Display(Name = "Size")]
        [StringLength(50)]
        public string Size { get; set; }
        
        [Display(Name = "Color")]
        [StringLength(50)]
        public string Color { get; set; }
        
        [Display(Name = "Color Hex Code")]
        [StringLength(7)]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Must be a valid hex color (e.g., #FF0000)")]
        public string ColorHex { get; set; }
        
        [Display(Name = "Material")]
        [StringLength(100)]
        public string Material { get; set; }
        
        [Required]
        [Display(Name = "Price (BGN)")]
        [Range(0.01, 999999)]
        public decimal Price { get; set; }
        
        [Required]
        [Display(Name = "Stock Quantity")]
        [Range(0, 999999)]
        public int StockQuantity { get; set; }
        
        [Display(Name = "Low Stock Threshold")]
        [Range(0, 1000)]
        public int LowStockThreshold { get; set; } = 10;
        
        [Display(Name = "Weight (grams)")]
        [Range(0, 50000)]
        public int WeightGrams { get; set; }
        
        [Display(Name = "Variant Image URL")]
        [StringLength(500)]
        public string ImageUrl { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }
    }

    public class AdminCollectionFormViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Collection Name")]
        [StringLength(200)]
        public string Name { get; set; }
        
        [Required]
        [Display(Name = "Description")]
        [StringLength(1000)]
        public string Description { get; set; }
        
        [Required]
        [Display(Name = "URL Slug")]
        [StringLength(200)]
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens")]
        public string Slug { get; set; }
        
        [Display(Name = "Banner Image URL")]
        [StringLength(500)]
        public string BannerImageUrl { get; set; }
        
        [Display(Name = "Season")]
        [StringLength(50)]
        public string Season { get; set; }
        
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }
        
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; }
        
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }
    }
}

