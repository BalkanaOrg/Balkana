namespace Balkana.Models.Store
{
    public class ProductDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public string SKU { get; set; }
        public decimal BasePrice { get; set; }
        public string MainImageUrl { get; set; }
        public List<string> AdditionalImages { get; set; } = new List<string>();
        
        // Category
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        
        // Team/Player
        public int? TeamId { get; set; }
        public string TeamName { get; set; }
        public string TeamTag { get; set; }
        public string TeamLogoUrl { get; set; }
        
        public int? PlayerId { get; set; }
        public string PlayerNickname { get; set; }
        public string PlayerFullName { get; set; }
        
        // Variants
        public List<ProductVariantViewModel> Variants { get; set; } = new List<ProductVariantViewModel>();
        
        // Collections
        public List<string> Collections { get; set; } = new List<string>();
        
        // Available options
        public List<string> AvailableSizes { get; set; } = new List<string>();
        public List<ColorOption> AvailableColors { get; set; } = new List<ColorOption>();
        
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductVariantViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SKU { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public string ColorHex { get; set; }
        public string Material { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool InStock => StockQuantity > 0;
        public bool LowStock => StockQuantity > 0 && StockQuantity <= LowStockThreshold;
        public int LowStockThreshold { get; set; }
        public string ImageUrl { get; set; }
        public int WeightGrams { get; set; }
    }

    public class ColorOption
    {
        public string ColorName { get; set; }
        public string ColorHex { get; set; }
        public int AvailableCount { get; set; }
    }
}

