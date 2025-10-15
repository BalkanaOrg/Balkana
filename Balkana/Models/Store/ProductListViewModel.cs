namespace Balkana.Models.Store
{
    public class ProductListViewModel
    {
        public List<ProductItemViewModel> Products { get; set; } = new List<ProductItemViewModel>();
        public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
        public List<CollectionViewModel> FeaturedCollections { get; set; } = new List<CollectionViewModel>();
        
        // Filters
        public int? CategoryId { get; set; }
        public int? CollectionId { get; set; }
        public string SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; } // "price-asc", "price-desc", "name", "newest"
        
        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalProducts { get; set; }
        public int PageSize { get; set; } = 12;
    }

    public class ProductItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public decimal BasePrice { get; set; }
        public decimal MinPrice { get; set; }  // Lowest variant price
        public decimal MaxPrice { get; set; }  // Highest variant price
        public string MainImageUrl { get; set; }
        public string CategoryName { get; set; }
        public bool IsFeatured { get; set; }
        public bool InStock { get; set; }
        public int VariantCount { get; set; }
        
        // Team/Player info
        public string TeamTag { get; set; }
        public string PlayerNickname { get; set; }
    }

    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public int ProductCount { get; set; }
        public List<CategoryViewModel> SubCategories { get; set; } = new List<CategoryViewModel>();
    }

    public class CollectionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string BannerImageUrl { get; set; }
        public string Season { get; set; }
        public int ProductCount { get; set; }
    }
}

