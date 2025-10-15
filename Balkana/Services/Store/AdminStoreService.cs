using Balkana.Data;
using Balkana.Data.Models.Store;
using Balkana.Models.Store;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Store
{
    public class AdminStoreService : IAdminStoreService
    {
        private readonly ApplicationDbContext _context;

        public AdminStoreService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Products
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Team)
                .Include(p => p.Player)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Include(p => p.Team)
                .Include(p => p.Player)
                .Include(p => p.ProductCollections)
                    .ThenInclude(pc => pc.Collection)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<int> CreateProductAsync(AdminProductFormViewModel model)
        {
            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                Slug = model.Slug,
                SKU = model.SKU,
                BasePrice = model.BasePrice,
                CategoryId = model.CategoryId,
                MainImageUrl = model.MainImageUrl,
                TeamId = model.TeamId,
                PlayerId = model.PlayerId,
                IsActive = model.IsActive,
                IsFeatured = model.IsFeatured,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return product.Id;
        }

        public async Task UpdateProductAsync(int id, AdminProductFormViewModel model)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new Exception("Product not found");

            product.Name = model.Name;
            product.Description = model.Description;
            product.Slug = model.Slug;
            product.SKU = model.SKU;
            product.BasePrice = model.BasePrice;
            product.CategoryId = model.CategoryId;
            product.MainImageUrl = model.MainImageUrl;
            product.TeamId = model.TeamId;
            product.PlayerId = model.PlayerId;
            product.IsActive = model.IsActive;
            product.IsFeatured = model.IsFeatured;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                throw new Exception("Product not found");

            // Soft delete by marking inactive
            product.IsActive = false;
            await _context.SaveChangesAsync();
        }

        // Variants
        public async Task<List<ProductVariant>> GetProductVariantsAsync(int productId)
        {
            return await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .OrderBy(v => v.DisplayOrder)
                .ToListAsync();
        }

        public async Task<int> CreateProductVariantAsync(AdminProductVariantFormViewModel model, string userId = null)
        {
            var variant = new ProductVariant
            {
                ProductId = model.ProductId,
                Name = model.Name,
                SKU = model.SKU,
                Size = model.Size,
                Color = model.Color,
                ColorHex = model.ColorHex,
                Material = model.Material,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                LowStockThreshold = model.LowStockThreshold,
                WeightGrams = model.WeightGrams,
                ImageUrl = model.ImageUrl,
                IsActive = model.IsActive,
                DisplayOrder = model.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            // Log initial stock
            if (variant.StockQuantity > 0)
            {
                _context.InventoryLogs.Add(new InventoryLog
                {
                    ProductVariantId = variant.Id,
                    QuantityChange = variant.StockQuantity,
                    StockAfter = variant.StockQuantity,
                    ChangeType = InventoryChangeType.InitialStock,
                    Notes = "Initial stock entry",
                    ChangedByUserId = userId,
                    ChangedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return variant.Id;
        }

        public async Task UpdateProductVariantAsync(int id, AdminProductVariantFormViewModel model)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null)
                throw new Exception("Variant not found");

            variant.Name = model.Name;
            variant.SKU = model.SKU;
            variant.Size = model.Size;
            variant.Color = model.Color;
            variant.ColorHex = model.ColorHex;
            variant.Material = model.Material;
            variant.Price = model.Price;
            variant.LowStockThreshold = model.LowStockThreshold;
            variant.WeightGrams = model.WeightGrams;
            variant.ImageUrl = model.ImageUrl;
            variant.IsActive = model.IsActive;
            variant.DisplayOrder = model.DisplayOrder;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductVariantAsync(int id)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null)
                throw new Exception("Variant not found");

            // Soft delete
            variant.IsActive = false;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStockAsync(int variantId, int quantity, string userId, string notes = null)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null)
                throw new Exception("Variant not found");

            var previousStock = variant.StockQuantity;
            var change = quantity - previousStock;

            variant.StockQuantity = quantity;

            _context.InventoryLogs.Add(new InventoryLog
            {
                ProductVariantId = variantId,
                QuantityChange = change,
                StockAfter = quantity,
                ChangeType = change > 0 ? InventoryChangeType.Restock : InventoryChangeType.Adjustment,
                Notes = notes ?? $"Stock adjusted from {previousStock} to {quantity}",
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        // Collections
        public async Task<List<Collection>> GetAllCollectionsAsync()
        {
            return await _context.Collections
                .Include(c => c.ProductCollections)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Collection> GetCollectionByIdAsync(int id)
        {
            return await _context.Collections
                .Include(c => c.ProductCollections)
                    .ThenInclude(pc => pc.Product)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<int> CreateCollectionAsync(AdminCollectionFormViewModel model)
        {
            var collection = new Collection
            {
                Name = model.Name,
                Description = model.Description,
                Slug = model.Slug,
                BannerImageUrl = model.BannerImageUrl,
                Season = model.Season,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsActive = model.IsActive,
                IsFeatured = model.IsFeatured,
                DisplayOrder = model.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            _context.Collections.Add(collection);
            await _context.SaveChangesAsync();

            return collection.Id;
        }

        public async Task UpdateCollectionAsync(int id, AdminCollectionFormViewModel model)
        {
            var collection = await _context.Collections.FindAsync(id);
            if (collection == null)
                throw new Exception("Collection not found");

            collection.Name = model.Name;
            collection.Description = model.Description;
            collection.Slug = model.Slug;
            collection.BannerImageUrl = model.BannerImageUrl;
            collection.Season = model.Season;
            collection.StartDate = model.StartDate;
            collection.EndDate = model.EndDate;
            collection.IsActive = model.IsActive;
            collection.IsFeatured = model.IsFeatured;
            collection.DisplayOrder = model.DisplayOrder;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteCollectionAsync(int id)
        {
            var collection = await _context.Collections.FindAsync(id);
            if (collection == null)
                throw new Exception("Collection not found");

            collection.IsActive = false;
            await _context.SaveChangesAsync();
        }

        public async Task AddProductToCollectionAsync(int collectionId, int productId)
        {
            var exists = await _context.ProductCollections
                .AnyAsync(pc => pc.CollectionId == collectionId && pc.ProductId == productId);

            if (!exists)
            {
                _context.ProductCollections.Add(new ProductCollection
                {
                    CollectionId = collectionId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveProductFromCollectionAsync(int collectionId, int productId)
        {
            var pc = await _context.ProductCollections
                .FirstOrDefaultAsync(pc => pc.CollectionId == collectionId && pc.ProductId == productId);

            if (pc != null)
            {
                _context.ProductCollections.Remove(pc);
                await _context.SaveChangesAsync();
            }
        }

        // Categories
        public async Task<List<ProductCategory>> GetAllCategoriesAsync()
        {
            return await _context.ProductCategories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        public async Task<int> CreateCategoryAsync(string name, string description, string slug, int? parentId = null)
        {
            var category = new ProductCategory
            {
                Name = name,
                Description = description,
                Slug = slug,
                ParentCategoryId = parentId,
                IsActive = true
            };

            _context.ProductCategories.Add(category);
            await _context.SaveChangesAsync();

            return category.Id;
        }

        // Orders
        public async Task<List<Order>> GetAllOrdersAsync(OrderStatus? statusFilter = null, int page = 1, int pageSize = 50)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (statusFilter.HasValue)
            {
                query = query.Where(o => o.Status == statusFilter.Value);
            }

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string adminNotes = null)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new Exception("Order not found");

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(adminNotes))
            {
                order.AdminNotes = (order.AdminNotes ?? "") + $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {adminNotes}";
            }

            if (newStatus == OrderStatus.Shipped)
            {
                order.ShippedDate = DateTime.UtcNow;
            }
            else if (newStatus == OrderStatus.Delivered)
            {
                order.DeliveredDate = DateTime.UtcNow;
            }
            else if (newStatus == OrderStatus.Cancelled)
            {
                order.CancelledAt = DateTime.UtcNow;
                // Restore stock
                foreach (var item in order.OrderItems)
                {
                    var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId);
                    if (variant != null)
                    {
                        variant.StockQuantity += item.Quantity;

                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductVariantId = variant.Id,
                            QuantityChange = item.Quantity,
                            StockAfter = variant.StockQuantity,
                            ChangeType = InventoryChangeType.Return,
                            OrderId = orderId,
                            Notes = $"Order {order.OrderNumber} cancelled - stock restored",
                            ChangedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdatePaymentStatusAsync(int orderId, PaymentStatus newStatus, string transactionId = null)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new Exception("Order not found");

            order.PaymentStatus = newStatus;
            order.PaymentTransactionId = transactionId;
            order.UpdatedAt = DateTime.UtcNow;

            if (newStatus == PaymentStatus.Paid)
            {
                order.PaymentDate = DateTime.UtcNow;
                
                // Auto-confirm order when paid
                if (order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Confirmed;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task AddTrackingNumberAsync(int orderId, string trackingNumber)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new Exception("Order not found");

            order.TrackingNumber = trackingNumber;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task CancelOrderAsync(int orderId, string reason)
        {
            await UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled, $"Cancelled: {reason}");
            
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.CancellationReason = reason;
                await _context.SaveChangesAsync();
            }
        }

        // Inventory
        public async Task<List<ProductVariant>> GetLowStockProductsAsync()
        {
            return await _context.ProductVariants
                .Include(v => v.Product)
                .Where(v => v.IsActive && v.StockQuantity <= v.LowStockThreshold && v.StockQuantity > 0)
                .OrderBy(v => v.StockQuantity)
                .ToListAsync();
        }

        public async Task<List<InventoryLog>> GetInventoryLogsAsync(int? productVariantId = null, int limit = 100)
        {
            var query = _context.InventoryLogs
                .Include(il => il.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(il => il.Order)
                .Include(il => il.ChangedByUser)
                .AsQueryable();

            if (productVariantId.HasValue)
            {
                query = query.Where(il => il.ProductVariantId == productVariantId.Value);
            }

            return await query
                .OrderByDescending(il => il.ChangedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}

