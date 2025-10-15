using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Balkana.ViewComponents
{
    public class UserProfilePictureViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserProfilePictureViewComponent(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return Content("");

            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (user == null)
                return Content("");

            var profilePictureUrl = !string.IsNullOrEmpty(user.ProfilePictureUrl) 
                ? user.ProfilePictureUrl 
                : "https://i.imgur.com/ZizgQGH.png";

            return View("Default", profilePictureUrl);
        }
    }
}
