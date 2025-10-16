using Balkana.Data;
using Balkana.Data.Models.Store;
using Balkana.Models.Store;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Store
{
    public class StoreService : IStoreService
    {
        private readonly ApplicationDbContext _context;

        public StoreService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProductListViewModel> GetProductsAsync(int? categoryId = null, int? collectionId = null,
            string searchTerm = null, decimal? minPrice = null, decimal? maxPrice = null,
            string sortBy = null, int page = 1, int pageSize = 12)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Team)
                .Include(p => p.Player)
                .Include(p => p.ProductCollections)
                .Where(p => p.IsActive);

            // Filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Filter by collection
            if (collectionId.HasValue)
            {
                query = query.Where(p => p.ProductCollections.Any(pc => pc.CollectionId == collectionId.Value));
            }

            // Search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            // Price filters
            if (minPrice.HasValue || maxPrice.HasValue)
            {
                query = query.Where(p => p.Variants.Any(v =>
                    (!minPrice.HasValue || v.Price >= minPrice.Value) &&
                    (!maxPrice.HasValue || v.Price <= maxPrice.Value)));
            }

            // Sorting
            query = sortBy switch
            {
                "price-asc" => query.OrderBy(p => p.Variants.Min(v => v.Price)),
                "price-desc" => query.OrderByDescending(p => p.Variants.Max(v => v.Price)),
                "name" => query.OrderBy(p => p.Name),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.Name)
            };

            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var productItems = products.Select(p => new ProductItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                BasePrice = p.BasePrice,
                MinPrice = p.Variants.Any() ? p.Variants.Min(v => v.Price) : p.BasePrice,
                MaxPrice = p.Variants.Any() ? p.Variants.Max(v => v.Price) : p.BasePrice,
                MainImageUrl = p.MainImageUrl,
                CategoryName = p.Category.Name,
                IsFeatured = p.IsFeatured,
                InStock = p.Variants.Any(v => v.StockQuantity > 0),
                VariantCount = p.Variants.Count,
                TeamTag = p.Team?.Tag,
                PlayerNickname = p.Player?.Nickname
            }).ToList();

            var categories = await GetCategoriesAsync();
            var featuredCollections = await GetCollectionsAsync(true);

            return new ProductListViewModel
            {
                Products = productItems,
                Categories = categories,
                FeaturedCollections = featuredCollections,
                CategoryId = categoryId,
                CollectionId = collectionId,
                SearchTerm = searchTerm,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalProducts = totalProducts,
                PageSize = pageSize
            };
        }

        public async Task<ProductDetailsViewModel> GetProductDetailsAsync(string slug)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants.OrderBy(v => v.DisplayOrder))
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(p => p.Team)
                .Include(p => p.Player)
                .Include(p => p.ProductCollections)
                    .ThenInclude(pc => pc.Collection)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null)
                return null;

            return MapToProductDetailsViewModel(product);
        }

        public async Task<ProductDetailsViewModel> GetProductDetailsByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants.OrderBy(v => v.DisplayOrder))
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(p => p.Team)
                .Include(p => p.Player)
                .Include(p => p.ProductCollections)
                    .ThenInclude(pc => pc.Collection)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return null;

            return MapToProductDetailsViewModel(product);
        }

        private ProductDetailsViewModel MapToProductDetailsViewModel(Product product)
        {
            return new ProductDetailsViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Slug = product.Slug,
                SKU = product.SKU,
                BasePrice = product.BasePrice,
                MainImageUrl = product.MainImageUrl,
                AdditionalImages = product.Images.Select(i => i.ImageUrl).ToList(),
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                TeamId = product.TeamId,
                TeamName = product.Team?.FullName,
                TeamTag = product.Team?.Tag,
                TeamLogoUrl = product.Team?.LogoURL,
                PlayerId = product.PlayerId,
                PlayerNickname = product.Player?.Nickname,
                PlayerFullName = product.Player != null ? $"{product.Player.FirstName} {product.Player.LastName}" : null,
                Variants = product.Variants.Where(v => v.IsActive).Select(v => new ProductVariantViewModel
                {
                    Id = v.Id,
                    Name = v.Name,
                    SKU = v.SKU,
                    Size = v.Size,
                    Color = v.Color,
                    ColorHex = v.ColorHex,
                    Material = v.Material,
                    Price = v.Price,
                    StockQuantity = v.StockQuantity,
                    LowStockThreshold = v.LowStockThreshold,
                    ImageUrl = v.ImageUrl,
                    WeightGrams = v.WeightGrams
                }).ToList(),
                Collections = product.ProductCollections.Select(pc => pc.Collection.Name).ToList(),
                AvailableSizes = product.Variants.Where(v => v.IsActive && !string.IsNullOrEmpty(v.Size))
                    .Select(v => v.Size).Distinct().ToList(),
                AvailableColors = product.Variants.Where(v => v.IsActive && !string.IsNullOrEmpty(v.Color))
                    .GroupBy(v => new { v.Color, v.ColorHex })
                    .Select(g => new ColorOption
                    {
                        ColorName = g.Key.Color,
                        ColorHex = g.Key.ColorHex,
                        AvailableCount = g.Count()
                    }).ToList(),
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                CreatedAt = product.CreatedAt
            };
        }

        public async Task<List<CategoryViewModel>> GetCategoriesAsync()
        {
            var categories = await _context.ProductCategories
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .Where(c => c.IsActive && c.ParentCategoryId == null)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return categories.Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ProductCount = c.Products.Count(p => p.IsActive),
                SubCategories = c.SubCategories.Select(sc => new CategoryViewModel
                {
                    Id = sc.Id,
                    Name = sc.Name,
                    Slug = sc.Slug,
                    ProductCount = sc.Products.Count(p => p.IsActive)
                }).ToList()
            }).ToList();
        }

        public async Task<List<CollectionViewModel>> GetCollectionsAsync(bool featuredOnly = false)
        {
            var query = _context.Collections
                .Include(c => c.ProductCollections)
                .Where(c => c.IsActive);

            if (featuredOnly)
            {
                query = query.Where(c => c.IsFeatured);
            }

            var collections = await query
                .OrderBy(c => c.DisplayOrder)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();

            return collections.Select(c => new CollectionViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                BannerImageUrl = c.BannerImageUrl,
                Season = c.Season,
                ProductCount = c.ProductCollections.Count
            }).ToList();
        }

        public async Task<CollectionViewModel> GetCollectionBySlugAsync(string slug)
        {
            var collection = await _context.Collections
                .Include(c => c.ProductCollections)
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);

            if (collection == null)
                return null;

            return new CollectionViewModel
            {
                Id = collection.Id,
                Name = collection.Name,
                Slug = collection.Slug,
                Description = collection.Description,
                BannerImageUrl = collection.BannerImageUrl,
                Season = collection.Season,
                ProductCount = collection.ProductCollections.Count
            };
        }

        public async Task<ShoppingCartViewModel> GetCartAsync(string userId)
        {
            var cart = await _context.ShoppingCarts
                .Include(sc => sc.Items)
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(sc => sc.UserId == userId);

            if (cart == null)
                return new ShoppingCartViewModel();

            return MapCartToViewModel(cart);
        }

        public async Task<ShoppingCartViewModel> GetGuestCartAsync(List<CartItemViewModel> sessionCart)
        {
            if (sessionCart == null || !sessionCart.Any())
                return new ShoppingCartViewModel();

            var variantIds = sessionCart.Select(i => i.ProductVariantId).ToList();
            
            var variants = await _context.ProductVariants
                .Include(v => v.Product)
                .Where(v => variantIds.Contains(v.Id))
                .ToListAsync();

            var items = new List<CartItemViewModel>();
            
            foreach (var sessionItem in sessionCart)
            {
                var variant = variants.FirstOrDefault(v => v.Id == sessionItem.ProductVariantId);
                if (variant != null)
                {
                    items.Add(new CartItemViewModel
                    {
                        Id = sessionItem.Id,
                        ProductId = variant.ProductId,
                        ProductVariantId = variant.Id,
                        ProductName = variant.Product.Name,
                        VariantName = variant.Name,
                        SKU = variant.SKU,
                        Size = variant.Size,
                        Color = variant.Color,
                        ColorHex = variant.ColorHex,
                        Price = variant.Price,
                        Quantity = sessionItem.Quantity,
                        ProductImageUrl = variant.ImageUrl ?? variant.Product.MainImageUrl,
                        StockAvailable = variant.StockQuantity,
                        ProductSlug = variant.Product.Slug
                    });
                }
            }

            return CalculateCartTotals(items);
        }

        public async Task AddToCartAsync(string userId, int productVariantId, int quantity = 1)
        {
            var variant = await _context.ProductVariants.FindAsync(productVariantId);
            if (variant == null || !variant.IsActive)
                throw new Exception("Product variant not found or inactive");

            if (variant.StockQuantity < quantity)
                throw new Exception($"Insufficient stock. Only {variant.StockQuantity} available.");

            var cart = await _context.ShoppingCarts
                .Include(sc => sc.Items)
                .FirstOrDefaultAsync(sc => sc.UserId == userId);

            if (cart == null)
            {
                cart = new ShoppingCart { UserId = userId };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductVariantId == productVariantId);
            
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                if (variant.StockQuantity < existingItem.Quantity)
                    throw new Exception($"Insufficient stock. Only {variant.StockQuantity} available.");
            }
            else
            {
                cart.Items.Add(new ShoppingCartItem
                {
                    ProductVariantId = productVariantId,
                    Quantity = quantity
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCartItemAsync(string userId, int cartItemId, int quantity)
        {
            var cartItem = await _context.ShoppingCartItems
                .Include(i => i.ShoppingCart)
                .Include(i => i.ProductVariant)
                .FirstOrDefaultAsync(i => i.Id == cartItemId && i.ShoppingCart.UserId == userId);

            if (cartItem == null)
                throw new Exception("Cart item not found");

            if (quantity <= 0)
            {
                _context.ShoppingCartItems.Remove(cartItem);
            }
            else
            {
                if (cartItem.ProductVariant.StockQuantity < quantity)
                    throw new Exception($"Insufficient stock. Only {cartItem.ProductVariant.StockQuantity} available.");

                cartItem.Quantity = quantity;
            }

            cartItem.ShoppingCart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(string userId, int cartItemId)
        {
            var cartItem = await _context.ShoppingCartItems
                .Include(i => i.ShoppingCart)
                .FirstOrDefaultAsync(i => i.Id == cartItemId && i.ShoppingCart.UserId == userId);

            if (cartItem != null)
            {
                _context.ShoppingCartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(string userId)
        {
            var cart = await _context.ShoppingCarts
                .Include(sc => sc.Items)
                .FirstOrDefaultAsync(sc => sc.UserId == userId);

            if (cart != null)
            {
                _context.ShoppingCartItems.RemoveRange(cart.Items);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> CreateOrderAsync(CheckoutViewModel checkout, string userId = null)
        {
            // Generate unique order number
            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = userId,
                GuestEmail = checkout.IsGuestCheckout ? checkout.Email : null,
                GuestFirstName = checkout.IsGuestCheckout ? checkout.FirstName : null,
                GuestLastName = checkout.IsGuestCheckout ? checkout.LastName : null,
                GuestPhone = checkout.Phone,
                SubTotal = checkout.Cart.SubTotal,
                ShippingCost = checkout.ShippingCost,
                Tax = checkout.Tax,
                TotalAmount = checkout.Cart.SubTotal + checkout.ShippingCost + checkout.Tax,
                Currency = checkout.Cart.Currency,
                Status = OrderStatus.Pending,
                PaymentMethod = checkout.PaymentMethod,
                PaymentStatus = checkout.PaymentMethod == PaymentMethod.CashOnDelivery 
                    ? PaymentStatus.Pending 
                    : PaymentStatus.Pending,
                DeliveryProvider = checkout.DeliveryProvider,
                ShippingAddress = checkout.ShippingAddress,
                ShippingCity = checkout.ShippingCity,
                ShippingPostalCode = checkout.ShippingPostalCode,
                ShippingCountry = checkout.ShippingCountry,
                DeliveryOfficeCode = checkout.DeliveryOfficeCode,
                DeliveryOfficeAddress = checkout.DeliveryOfficeAddress,
                BillingAddress = checkout.BillingSameAsShipping ? checkout.ShippingAddress : checkout.BillingAddress,
                BillingCity = checkout.BillingSameAsShipping ? checkout.ShippingCity : checkout.BillingCity,
                BillingPostalCode = checkout.BillingSameAsShipping ? checkout.ShippingPostalCode : checkout.BillingPostalCode,
                BillingCountry = checkout.BillingSameAsShipping ? checkout.ShippingCountry : checkout.BillingCountry,
                CustomerNotes = checkout.CustomerNotes,
                CreatedAt = DateTime.UtcNow
            };

            // Add order items from cart
            foreach (var cartItem in checkout.Cart.Items)
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == cartItem.ProductVariantId);

                if (variant == null || variant.StockQuantity < cartItem.Quantity)
                    throw new Exception($"Insufficient stock for {cartItem.ProductName}");

                order.OrderItems.Add(new OrderItem
                {
                    ProductVariantId = variant.Id,
                    ProductName = variant.Product.Name,
                    VariantName = variant.Name,
                    SKU = variant.SKU,
                    UnitPrice = variant.Price,
                    Quantity = cartItem.Quantity,
                    TotalPrice = variant.Price * cartItem.Quantity,
                    ProductImageUrl = variant.ImageUrl ?? variant.Product.MainImageUrl
                });

                // Reduce stock
                variant.StockQuantity -= cartItem.Quantity;

                // Log inventory change
                _context.InventoryLogs.Add(new InventoryLog
                {
                    ProductVariantId = variant.Id,
                    QuantityChange = -cartItem.Quantity,
                    StockAfter = variant.StockQuantity,
                    ChangeType = InventoryChangeType.Sale,
                    Notes = $"Order {orderNumber}",
                    ChangedAt = DateTime.UtcNow
                });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Clear cart for registered users
            if (!string.IsNullOrEmpty(userId))
            {
                await ClearCartAsync(userId);
            }

            return orderNumber;
        }

        public async Task<OrderViewModel> GetOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return null;

            return MapOrderToViewModel(order);
        }

        public async Task<OrderViewModel> GetOrderByNumberAsync(string orderNumber)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null)
                return null;

            return MapOrderToViewModel(order);
        }

        public async Task<List<OrderViewModel>> GetUserOrdersAsync(string userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o => MapOrderToViewModel(o)).ToList();
        }

        public async Task<bool> CheckStockAvailabilityAsync(int productVariantId, int quantity)
        {
            var variant = await _context.ProductVariants.FindAsync(productVariantId);
            return variant != null && variant.StockQuantity >= quantity;
        }

        public async Task<Dictionary<int, int>> CheckMultipleStockAvailabilityAsync(Dictionary<int, int> variantQuantities)
        {
            var variantIds = variantQuantities.Keys.ToList();
            var variants = await _context.ProductVariants
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.StockQuantity);

            var availability = new Dictionary<int, int>();
            foreach (var kvp in variantQuantities)
            {
                availability[kvp.Key] = variants.ContainsKey(kvp.Key) ? variants[kvp.Key] : 0;
            }

            return availability;
        }

        private ShoppingCartViewModel MapCartToViewModel(ShoppingCart cart)
        {
            var items = cart.Items.Select(i => new CartItemViewModel
            {
                Id = i.Id,
                ProductId = i.ProductVariant.ProductId,
                ProductVariantId = i.ProductVariantId,
                ProductName = i.ProductVariant.Product.Name,
                VariantName = i.ProductVariant.Name,
                SKU = i.ProductVariant.SKU,
                Size = i.ProductVariant.Size,
                Color = i.ProductVariant.Color,
                ColorHex = i.ProductVariant.ColorHex,
                Price = i.ProductVariant.Price,
                Quantity = i.Quantity,
                ProductImageUrl = i.ProductVariant.ImageUrl ?? i.ProductVariant.Product.MainImageUrl,
                StockAvailable = i.ProductVariant.StockQuantity,
                ProductSlug = i.ProductVariant.Product.Slug
            }).ToList();

            return CalculateCartTotals(items);
        }

        private ShoppingCartViewModel CalculateCartTotals(List<CartItemViewModel> items)
        {
            var subTotal = items.Sum(i => i.Total);
            var estimatedTax = 0; // VAT already included in prices

            return new ShoppingCartViewModel
            {
                Items = items,
                SubTotal = subTotal,
                EstimatedShipping = 0, // Calculate based on weight/location
                EstimatedTax = estimatedTax,
                EstimatedTotal = subTotal + estimatedTax,
                TotalItems = items.Sum(i => i.Quantity),
                Currency = "EUR"
            };
        }

        private OrderViewModel MapOrderToViewModel(Order order)
        {
            var customerName = !string.IsNullOrEmpty(order.GuestFirstName)
                ? $"{order.GuestFirstName} {order.GuestLastName}"
                : order.User != null ? $"{order.User.FirstName} {order.User.LastName}" : "Unknown";

            var customerEmail = !string.IsNullOrEmpty(order.GuestEmail)
                ? order.GuestEmail
                : order.User?.Email;

            return new OrderViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                CustomerPhone = order.GuestPhone,
                IsGuestOrder = !string.IsNullOrEmpty(order.GuestEmail),
                Items = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    Id = oi.Id,
                    ProductName = oi.ProductName,
                    VariantName = oi.VariantName,
                    SKU = oi.SKU,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    TotalPrice = oi.TotalPrice,
                    ProductImageUrl = oi.ProductImageUrl,
                    ProductId = oi.ProductVariant?.ProductId ?? 0,
                    ProductSlug = oi.ProductVariant?.Product?.Slug
                }).ToList(),
                SubTotal = order.SubTotal,
                ShippingCost = order.ShippingCost,
                Tax = order.Tax,
                Discount = order.Discount,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                Status = order.Status,
                StatusDisplay = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus,
                PaymentStatusDisplay = order.PaymentStatus.ToString(),
                PaymentMethod = order.PaymentMethod,
                PaymentMethodDisplay = order.PaymentMethod.ToString(),
                PaymentTransactionId = order.PaymentTransactionId,
                DeliveryProvider = order.DeliveryProvider,
                DeliveryProviderDisplay = order.DeliveryProvider.ToString(),
                TrackingNumber = order.TrackingNumber,
                ShippingAddress = order.ShippingAddress,
                ShippingCity = order.ShippingCity,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountry = order.ShippingCountry,
                DeliveryOfficeAddress = order.DeliveryOfficeAddress,
                CreatedAt = order.CreatedAt,
                PaymentDate = order.PaymentDate,
                ShippedDate = order.ShippedDate,
                DeliveredDate = order.DeliveredDate,
                CancelledAt = order.CancelledAt,
                CustomerNotes = order.CustomerNotes,
                AdminNotes = order.AdminNotes,
                CancellationReason = order.CancellationReason
            };
        }
    }
}

