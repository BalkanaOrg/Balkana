namespace Balkana.Data.Models
{
    public class ItemReference
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; } // relative path, e.g. "3071.png"
        public string PatchVersion { get; set; } // e.g. "15.16.1"
    }
}
