using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using static Balkana.Data.DataConstants;

namespace Balkana.Services.Brandings.Models
{
    public class BrandingFormModel : IBrandingModel
    {
        [Required]
        [Display(Name = "Brand full name")]
        [StringLength(TeamFullNameMaxLength, MinimumLength = TeamFullNameMinLength)]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Brand tag")]
        [StringLength(TeamTagMaxLength, MinimumLength = TeamTagMinLength)]
        public string Tag { get; set; }

        [Display(Name = "Brand Logo")]
        public IFormFile? LogoFile { get; set; }

        public string? LogoPath { get; set; }

        [Required]
        [Display(Name = "Year Founded")]
        public int YearFounded { get; set; }

        [Display(Name = "Founder")]
        public string? FounderId { get; set; }
        
        [Display(Name = "Founder Name")]
        public string? FounderName { get; set; }

        [Display(Name = "Manager")]
        public string? ManagerId { get; set; }
        
        [Display(Name = "Manager Name")]
        public string? ManagerName { get; set; }
    }
}

