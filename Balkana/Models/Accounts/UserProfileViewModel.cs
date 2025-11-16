using Balkana.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Accounts
{
    public class UserProfileViewModel
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public int NationalityId { get; set; }
        public Nationality Nationality { get; set; }
        public int? PlayerId { get; set; }
        public Player Player { get; set; }
        public bool HasPlayerProfile => PlayerId.HasValue && Player != null;
        public IEnumerable<Nationality> Nationalities { get; set; } = new List<Nationality>();
        public bool IsCurrentUser { get; set; }
        public string UserStatus { get; set; }
        public IEnumerable<string> UserRoles { get; set; } = new List<string>();
    }


    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least 6 characters long", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class UploadProfilePictureViewModel
    {
        [Required(ErrorMessage = "Profile picture is required")]
        public IFormFile ProfilePicture { get; set; }
    }

    public class SearchUsersViewModel
    {
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalUsers { get; set; }
        public IEnumerable<UserProfileViewModel> Users { get; set; } = new List<UserProfileViewModel>();
        public const int UsersPerPage = 12;
    }
}
