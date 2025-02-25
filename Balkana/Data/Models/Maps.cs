namespace Balkana.Data.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using static DataConstants;

    public class Maps
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(defaultStringMaxLength)]
        [MinLength(defaultStringMinLength)]
        public string Name { get; set; }
        public string Picture { get; set; }
        public bool isActiveDuty { get; set; }
    }
}
