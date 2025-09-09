using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Community
{
    public class CreateCommunityTeamModel
    {
        [Required]
        [StringLength(50)]
        public string FullName { get; set; }

        [Required]
        [StringLength(10)]
        public string Tag { get; set; }

        public string LogoUrl { get; set; }

        [Required]
        [Display(Name = "Game")]
        public int GameId { get; set; }

        public IEnumerable<SelectListItem> Games { get; set; } = new List<SelectListItem>();
    }
}
