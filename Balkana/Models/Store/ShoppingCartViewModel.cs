using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Models.Store
{
    public class ShoppingCartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal SubTotal { get; set; }
        public decimal EstimatedShipping { get; set; }
        public decimal EstimatedTax { get; set; }
        public decimal EstimatedTotal { get; set; }
        public int TotalItems { get; set; }
        public string Currency { get; set; } = "BGN";
    }

    public class CartItemViewModel
    {
        public int Id { get; set; }  // CartItem ID
        public int ProductId { get; set; }
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; }
        public string VariantName { get; set; }
        public string SKU { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public string ColorHex { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
        public string ProductImageUrl { get; set; }
        public int StockAvailable { get; set; }
        public bool InStock => StockAvailable >= Quantity;
        public string ProductSlug { get; set; }
    }
}

