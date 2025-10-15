namespace Balkana.Data.Models.Store
{
    /// <summary>
    /// Many-to-many relationship between Products and Collections
    /// </summary>
    public class ProductCollection
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int CollectionId { get; set; }
        public Collection Collection { get; set; }

        /// <summary>
        /// Display order within the collection
        /// </summary>
        public int DisplayOrder { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}

