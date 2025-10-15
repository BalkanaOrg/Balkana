using Balkana.Data.Models.Store;
using Balkana.Models.Store;

namespace Balkana.Services.Store
{
    public interface IAdminStoreService
    {
        // Product management
        Task<List<Product>> GetAllProductsAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<int> CreateProductAsync(AdminProductFormViewModel model);
        Task UpdateProductAsync(int id, AdminProductFormViewModel model);
        Task DeleteProductAsync(int id);
        
        // Variant management
        Task<List<ProductVariant>> GetProductVariantsAsync(int productId);
        Task<int> CreateProductVariantAsync(AdminProductVariantFormViewModel model, string userId = null);
        Task UpdateProductVariantAsync(int id, AdminProductVariantFormViewModel model);
        Task DeleteProductVariantAsync(int id);
        Task UpdateStockAsync(int variantId, int quantity, string userId, string notes = null);
        
        // Collection management
        Task<List<Collection>> GetAllCollectionsAsync();
        Task<Collection> GetCollectionByIdAsync(int id);
        Task<int> CreateCollectionAsync(AdminCollectionFormViewModel model);
        Task UpdateCollectionAsync(int id, AdminCollectionFormViewModel model);
        Task DeleteCollectionAsync(int id);
        Task AddProductToCollectionAsync(int collectionId, int productId);
        Task RemoveProductFromCollectionAsync(int collectionId, int productId);
        
        // Category management
        Task<List<ProductCategory>> GetAllCategoriesAsync();
        Task<int> CreateCategoryAsync(string name, string description, string slug, int? parentId = null);
        
        // Order management
        Task<List<Order>> GetAllOrdersAsync(OrderStatus? statusFilter = null, int page = 1, int pageSize = 50);
        Task<Order> GetOrderByIdAsync(int id);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string adminNotes = null);
        Task UpdatePaymentStatusAsync(int orderId, PaymentStatus newStatus, string transactionId = null);
        Task AddTrackingNumberAsync(int orderId, string trackingNumber);
        Task CancelOrderAsync(int orderId, string reason);
        
        // Inventory
        Task<List<ProductVariant>> GetLowStockProductsAsync();
        Task<List<InventoryLog>> GetInventoryLogsAsync(int? productVariantId = null, int limit = 100);
    }
}

