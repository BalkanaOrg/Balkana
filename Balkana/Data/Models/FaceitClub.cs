using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    public class FaceitClub
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string FaceitId { get; set; }
    }
}
