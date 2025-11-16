using Balkana.Data.Models.Store;
using Balkana.Models.Store;

namespace Balkana.Services.Store
{
    public interface IStoreService
    {
        // Product browsing
        Task<ProductListViewModel> GetProductsAsync(int? categoryId = null, int? collectionId = null, 
            string searchTerm = null, decimal? minPrice = null, decimal? maxPrice = null, 
            string sortBy = null, int page = 1, int pageSize = 12);
        
        Task<ProductDetailsViewModel> GetProductDetailsAsync(string slug);
        Task<ProductDetailsViewModel> GetProductDetailsByIdAsync(int id);
        
        // Categories & Collections
        Task<List<CategoryViewModel>> GetCategoriesAsync();
        Task<List<CollectionViewModel>> GetCollectionsAsync(bool featuredOnly = false);
        Task<CollectionViewModel> GetCollectionBySlugAsync(string slug);
        
        // Cart management (session-based for guests, DB for registered users)
        Task<ShoppingCartViewModel> GetCartAsync(string userId);
        Task<ShoppingCartViewModel> GetGuestCartAsync(List<CartItemViewModel> sessionCart);
        Task AddToCartAsync(string userId, int productVariantId, int quantity = 1);
        Task UpdateCartItemAsync(string userId, int cartItemId, int quantity);
        Task RemoveFromCartAsync(string userId, int cartItemId);
        Task ClearCartAsync(string userId);
        
        // Order processing
        Task<string> CreateOrderAsync(CheckoutViewModel checkout, string userId = null);
        Task<OrderViewModel> GetOrderAsync(int orderId);
        Task<OrderViewModel> GetOrderByNumberAsync(string orderNumber);
        Task<List<OrderViewModel>> GetUserOrdersAsync(string userId);
        
        // Stock checking
        Task<bool> CheckStockAvailabilityAsync(int productVariantId, int quantity);
        Task<Dictionary<int, int>> CheckMultipleStockAvailabilityAsync(Dictionary<int, int> variantQuantities);
    }
}

