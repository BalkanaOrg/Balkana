using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models.Store
{
    /// <summary>
    /// Shopping cart for registered users (session-based for guests)
    /// </summary>
    public class ShoppingCart
    {
        public int Id { get; set; }

        /// <summary>
        /// User ID for registered users
        /// </summary>
        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
    }
}

