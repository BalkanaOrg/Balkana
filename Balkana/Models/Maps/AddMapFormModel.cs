using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Linq;

namespace Balkana.Models.Maps
{
    using static Data.DataConstants;
    public class AddMapFormModel
    {
        [Required]
        [Display(Name = "Region's full name")]
        [StringLength(defaultStringMaxLength, MinimumLength = defaultStringMinLength)]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Region's short name")]
        [MinLength(3)]
        public string PictureURL { get; set; }

        [Required]
        public bool isActiveDuty { get; set; }
    }
}
