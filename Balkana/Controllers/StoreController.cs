using Balkana.Data;
using Balkana.Data.Models.Store;
using Balkana.Models.Store;
using Balkana.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Balkana.Controllers
{
    public class StoreController : Controller
    {
        private readonly IStoreService _storeService;
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "GuestCart";

        public StoreController(IStoreService storeService, ApplicationDbContext context)
        {
            _storeService = storeService;
            _context = context;
        }

        // GET: /Store
        public async Task<IActionResult> Index(int? categoryId, int? collectionId, string search, 
            decimal? minPrice, decimal? maxPrice, string sort, int page = 1)
        {
            var model = await _storeService.GetProductsAsync(categoryId, collectionId, search, 
                minPrice, maxPrice, sort, page);
            
            return View(model);
        }

        // GET: /Store/Product/{slug}
        [Route("store/product/{slug}")]
        public async Task<IActionResult> Product(string slug)
        {
            var product = await _storeService.GetProductDetailsAsync(slug);
            
            if (product == null)
                return NotFound();
            
            return View(product);
        }

        // GET: /Store/Collection/{slug}
        [Route("store/collection/{slug}")]
        public async Task<IActionResult> Collection(string slug)
        {
            var collection = await _storeService.GetCollectionBySlugAsync(slug);
            
            if (collection == null)
                return NotFound();
            
            var products = await _storeService.GetProductsAsync(collectionId: collection.Id);
            ViewBag.Collection = collection;
            
            return View(products);
        }

        // GET: /Store/Category/{slug}
        [Route("store/category/{slug}")]
        public async Task<IActionResult> Category(string slug)
        {
            // Find category by slug
            var categories = await _storeService.GetCategoriesAsync();
            var category = categories.FirstOrDefault(c => c.Slug == slug);
            
            if (category == null)
                return NotFound();
            
            var products = await _storeService.GetProductsAsync(categoryId: category.Id);
            ViewBag.Category = category;
            
            return View(products);
        }

        // ========================================
        // SHOPPING CART
        // ========================================

        // GET: /Store/Cart
        public async Task<IActionResult> Cart()
        {
            ShoppingCartViewModel cart;

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                cart = await _storeService.GetCartAsync(userId);
            }
            else
            {
                var sessionCart = GetGuestCart();
                cart = await _storeService.GetGuestCartAsync(sessionCart);
            }

            return View(cart);
        }

        // POST: /Store/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productVariantId, int quantity = 1)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    await _storeService.AddToCartAsync(userId, productVariantId, quantity);
                }
                else
                {
                    // Add to session cart
                    var cart = GetGuestCart();
                    var existing = cart.FirstOrDefault(i => i.ProductVariantId == productVariantId);
                    
                    if (existing != null)
                    {
                        existing.Quantity += quantity;
                    }
                    else
                    {
                        cart.Add(new CartItemViewModel
                        {
                            Id = cart.Any() ? cart.Max(i => i.Id) + 1 : 1,
                            ProductVariantId = productVariantId,
                            Quantity = quantity
                        });
                    }
                    
                    SaveGuestCart(cart);
                }

                TempData["SuccessMessage"] = "Product added to cart!";
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: /Store/UpdateCartItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, int quantity)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    await _storeService.UpdateCartItemAsync(userId, cartItemId, quantity);
                }
                else
                {
                    var cart = GetGuestCart();
                    var item = cart.FirstOrDefault(i => i.Id == cartItemId);
                    
                    if (item != null)
                    {
                        if (quantity <= 0)
                        {
                            cart.Remove(item);
                        }
                        else
                        {
                            item.Quantity = quantity;
                        }
                        SaveGuestCart(cart);
                    }
                }

                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Cart");
            }
        }

        // POST: /Store/RemoveFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    await _storeService.RemoveFromCartAsync(userId, cartItemId);
                }
                else
                {
                    var cart = GetGuestCart();
                    var item = cart.FirstOrDefault(i => i.Id == cartItemId);
                    if (item != null)
                    {
                        cart.Remove(item);
                        SaveGuestCart(cart);
                    }
                }

                TempData["SuccessMessage"] = "Item removed from cart";
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Cart");
            }
        }

        // Guest cart helpers
        private List<CartItemViewModel> GetGuestCart()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            return string.IsNullOrEmpty(cartJson)
                ? new List<CartItemViewModel>()
                : JsonConvert.DeserializeObject<List<CartItemViewModel>>(cartJson);
        }

        private void SaveGuestCart(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonConvert.SerializeObject(cart));
        }

        private void ClearGuestCart()
        {
            HttpContext.Session.Remove(CartSessionKey);
        }

        // ========================================
        // CHECKOUT
        // ========================================

        // GET: /Store/Checkout
        public async Task<IActionResult> Checkout()
        {
            ShoppingCartViewModel cart;

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                cart = await _storeService.GetCartAsync(userId);
            }
            else
            {
                var sessionCart = GetGuestCart();
                cart = await _storeService.GetGuestCartAsync(sessionCart);
            }

            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty";
                return RedirectToAction("Cart");
            }

            // Check stock availability
            foreach (var item in cart.Items)
            {
                if (!item.InStock)
                {
                    TempData["ErrorMessage"] = $"{item.ProductName} ({item.VariantName}) is out of stock";
                    return RedirectToAction("Cart");
                }
            }

            var model = new CheckoutViewModel
            {
                Cart = cart,
                IsGuestCheckout = !User.Identity.IsAuthenticated,
                ShippingCountry = "Bulgaria",
                BillingSameAsShipping = true,
                DeliveryMethods = GetDeliveryMethods(),
                PaymentMethods = GetPaymentMethods(),
                EkontOffices = GetEkontOffices(),
                SpeedyOffices = GetSpeedyOffices(),
                Countries = GetCountries()
            };

            // Pre-fill user data if logged in
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _context.Users.Include(u => u.Nationality).FirstOrDefaultAsync(u => u.Id == userId);
                
                if (user != null)
                {
                    model.Email = user.Email;
                    model.FirstName = user.FirstName;
                    model.LastName = user.LastName;
                    model.Phone = user.PhoneNumber;
                }
            }

            return View(model);
        }

        // POST: /Store/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            // Re-load cart (not from form post)
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                model.Cart = await _storeService.GetCartAsync(userId);
            }
            else
            {
                var sessionCart = GetGuestCart();
                model.Cart = await _storeService.GetGuestCartAsync(sessionCart);
            }

            if (model.Cart == null || !model.Cart.Items.Any())
            {
                ModelState.AddModelError("", "Your cart is empty");
                model.DeliveryMethods = GetDeliveryMethods();
                model.PaymentMethods = GetPaymentMethods();
                model.EkontOffices = GetEkontOffices();
                model.SpeedyOffices = GetSpeedyOffices();
                model.Countries = GetCountries();
                return View(model);
            }

            // Remove Cart validation errors since it's loaded server-side
            ModelState.Remove("Cart");
            
            // Remove billing validation if BillingSameAsShipping is true
            if (model.BillingSameAsShipping)
            {
                ModelState.Remove("BillingAddress");
                ModelState.Remove("BillingCity");
                ModelState.Remove("BillingPostalCode");
                ModelState.Remove("BillingCountry");
            }

            if (!ModelState.IsValid)
            {
                model.DeliveryMethods = GetDeliveryMethods();
                model.PaymentMethods = GetPaymentMethods();
                model.EkontOffices = GetEkontOffices();
                model.SpeedyOffices = GetSpeedyOffices();
                model.Countries = GetCountries();
                return View(model);
            }

            try
            {
                // Calculate shipping
                model.ShippingCost = CalculateShipping(model.DeliveryProvider, model.Cart);
                model.Tax = 0; // VAT already included in prices

                var userId = User.Identity.IsAuthenticated 
                    ? User.FindFirstValue(ClaimTypes.NameIdentifier) 
                    : null;

                var orderNumber = await _storeService.CreateOrderAsync(model, userId);

                // Clear cart
                if (User.Identity.IsAuthenticated)
                {
                    await _storeService.ClearCartAsync(userId);
                }
                else
                {
                    ClearGuestCart();
                }

                TempData["SuccessMessage"] = $"Order placed successfully! Order number: {orderNumber}";
                return RedirectToAction("OrderConfirmation", new { orderNumber });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error processing order: {ex.Message}");
                model.DeliveryMethods = GetDeliveryMethods();
                model.PaymentMethods = GetPaymentMethods();
                model.EkontOffices = GetEkontOffices();
                model.SpeedyOffices = GetSpeedyOffices();
                model.Countries = GetCountries();
                return View(model);
            }
        }

        // GET: /Store/OrderConfirmation/{orderNumber}
        [Route("store/order-confirmation/{orderNumber}")]
        public async Task<IActionResult> OrderConfirmation(string orderNumber)
        {
            var order = await _storeService.GetOrderByNumberAsync(orderNumber);
            
            if (order == null)
                return NotFound();
            
            return View(order);
        }

        // GET: /Store/MyOrders
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _storeService.GetUserOrdersAsync(userId);
            
            return View(orders);
        }

        // GET: /Store/Order/{orderNumber}
        [Route("store/order/{orderNumber}")]
        public async Task<IActionResult> Order(string orderNumber, string email = null)
        {
            var order = await _storeService.GetOrderByNumberAsync(orderNumber);
            
            if (order == null)
                return NotFound();

            // Verify access
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                // User can access their own orders
                if (!order.IsGuestOrder && order.CustomerEmail != User.FindFirstValue(ClaimTypes.Email))
                {
                    return Forbid();
                }
            }
            else
            {
                // Guest needs to provide email
                if (string.IsNullOrEmpty(email) || email != order.CustomerEmail)
                {
                    return RedirectToAction("OrderLookup");
                }
            }
            
            return View(order);
        }

        // GET: /Store/OrderLookup (for guests)
        public IActionResult OrderLookup()
        {
            return View();
        }

        // POST: /Store/OrderLookup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OrderLookup(string orderNumber, string email)
        {
            if (string.IsNullOrWhiteSpace(orderNumber) || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Please provide both order number and email");
                return View();
            }

            return RedirectToAction("Order", new { orderNumber, email });
        }

        // Helpers
        private decimal CalculateShipping(DeliveryProvider provider, ShoppingCartViewModel cart)
        {
            // Calculate based on provider and total weight
            var totalWeight = cart.Items.Sum(i => i.Quantity * 100); // Assuming 100g per item as default

            return provider switch
            {
                DeliveryProvider.Ekont => 2.50m,           // Ekont office pickup
                DeliveryProvider.Speedy => 2.75m,         // Speedy office pickup
                DeliveryProvider.CourierToAddress => 4.00m, // Courier delivery
                DeliveryProvider.OfficePickup => 0.00m,    // Free pickup
                DeliveryProvider.InternationalShipping => 12.50m,
                _ => 2.50m
            };
        }

        private List<SelectListItem> GetDeliveryMethods()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = ((int)DeliveryProvider.Ekont).ToString(), Text = "Ekont - Office Pickup (2.50 EUR)" },
                new SelectListItem { Value = ((int)DeliveryProvider.Speedy).ToString(), Text = "Speedy - Office Pickup (2.75 EUR)" },
                new SelectListItem { Value = ((int)DeliveryProvider.CourierToAddress).ToString(), Text = "Courier to Address (4.00 EUR)" },
                new SelectListItem { Value = ((int)DeliveryProvider.OfficePickup).ToString(), Text = "Pickup from Balkana Office (Free)" }
            };
        }

        private List<SelectListItem> GetPaymentMethods()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = ((int)PaymentMethod.CashOnDelivery).ToString(), Text = "Cash on Delivery (Наложен платеж)" },
                new SelectListItem { Value = ((int)PaymentMethod.BankTransfer).ToString(), Text = "Bank Transfer" },
                new SelectListItem { Value = ((int)PaymentMethod.CreditCard).ToString(), Text = "Credit Card" },
                new SelectListItem { Value = ((int)PaymentMethod.DebitCard).ToString(), Text = "Debit Card" },
                new SelectListItem { Value = ((int)PaymentMethod.ePay).ToString(), Text = "ePay (Bulgarian Gateway)" },
                new SelectListItem { Value = ((int)PaymentMethod.Stripe).ToString(), Text = "Stripe" },
                new SelectListItem { Value = ((int)PaymentMethod.PayPal).ToString(), Text = "PayPal" }
            };
        }

        private List<SelectListItem> GetEkontOffices()
        {
            // In production, this would query Ekont API for offices in the selected city
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "EKONT-SOF-001", Text = "Sofia - Center (бул. Витоша 1)" },
                new SelectListItem { Value = "EKONT-SOF-002", Text = "Sofia - Lyulin (ж.к. Люлин 5)" },
                new SelectListItem { Value = "EKONT-PLV-001", Text = "Plovdiv - Center (ул. Главна 15)" },
                new SelectListItem { Value = "EKONT-VAR-001", Text = "Varna - Center (бул. Владислав Варненчик 20)" }
            };
        }

        private List<SelectListItem> GetSpeedyOffices()
        {
            // In production, this would query Speedy API for offices
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "SPEEDY-SOF-001", Text = "Sofia - NDK (бул. България 1)" },
                new SelectListItem { Value = "SPEEDY-SOF-002", Text = "Sofia - Druzhba (ж.к. Дружба 2)" },
                new SelectListItem { Value = "SPEEDY-PLV-001", Text = "Plovdiv - Kapana (ул. Кап. Райчо 5)" },
                new SelectListItem { Value = "SPEEDY-VAR-001", Text = "Varna - Sea Garden (Морска градина)" }
            };
        }

        private List<SelectListItem> GetCountries()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Bulgaria", Text = "Bulgaria (България)", Selected = true },
                new SelectListItem { Value = "Romania", Text = "Romania (România)" },
                new SelectListItem { Value = "Greece", Text = "Greece (Ελλάδα)" },
                new SelectListItem { Value = "Serbia", Text = "Serbia (Србија)" },
                new SelectListItem { Value = "North Macedonia", Text = "North Macedonia (Северна Македонија)" }
            };
        }
    }
}
