namespace Balkana.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using static DataConstants;
    
    public class Branding
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(TeamTagMaxLength)]
        [MinLength(TeamTagMinLength)]
        public string Tag { get; set; }

        [Required]
        [MaxLength(TeamFullNameMaxLength)]
        [MinLength(TeamFullNameMinLength)]
        public string FullName { get; set; }

        [Required]
        public int yearFounded { get; set; }

        [Required]
        public string LogoURL { get; set; }

        public string? FounderId { get; set; }
        [ForeignKey(nameof(FounderId))]
        public ApplicationUser? Founder { get; set; }

        public string? ManagerId { get; set; }
        [ForeignKey(nameof(ManagerId))]
        public ApplicationUser? Manager { get; set; }

        public ICollection<Team> Teams { get; set; } = new List<Team>();
    }
}

