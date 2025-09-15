using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Admin
{
    public class AddGameProfileViewModel
    {
        [Required(ErrorMessage = "Please select a player.")]
        public int? SelectedPlayerId { get; set; }

        // Used to render the selected player's nickname if we re-display the form after a validation error
        [BindNever]
        public string? SelectedPlayerText { get; set; }

        [Required]
        public string Provider { get; set; }

        [Required]
        public string UUID { get; set; }

        public List<SelectListItem> Providers { get; set; } = new()
        {
            new SelectListItem { Value = "FACEIT", Text = "FACEIT" },
            new SelectListItem { Value = "RIOT", Text = "RIOT" }
        };
    }
}
