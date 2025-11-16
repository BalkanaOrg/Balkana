using Balkana.Data.Models;
using Balkana.Models.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Balkana.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Security.Claims;

namespace Balkana.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                NationalityId = model.NationalityId,
                ProfilePictureUrl = model.ProfilePictureUrl
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Member"); // default role
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToLocal(returnUrl);

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile(string id = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // If no ID provided, redirect to current user's profile
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction(nameof(Profile), new { id = currentUserId });
            }
            
            var userId = id;
            var user = await _context.Users
                .Include(u => u.Nationality)
                .Include(u => u.Player)
                    .ThenInclude(p => p.PlayerTrophies)
                        .ThenInclude(pt => pt.Trophy)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            // Get user roles
            var userRoles = await _userManager.GetRolesAsync(user);
            
            // Determine user status based on role priority
            var status = DetermineUserStatus(userRoles, user.PlayerId.HasValue);

            var model = new UserProfileViewModel
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                NationalityId = user.NationalityId,
                Nationality = user.Nationality,
                PlayerId = user.PlayerId,
                Player = user.Player,
                Nationalities = await _context.Nationalities.ToListAsync(),
                IsCurrentUser = userId == currentUserId,
                UserStatus = status,
                UserRoles = userRoles
            };

            return View(model);
        }

        private string DetermineUserStatus(IEnumerable<string> roles, bool hasPlayerProfile)
        {
            // Role priority: Administrator > Moderator > Editor > Author > StoreManager > Pro Player > Member
            var rolePriority = new Dictionary<string, int>
            {
                { "Administrator", 1 },
                { "Moderator", 2 },
                { "Editor", 3 },
                { "Author", 4 },
                { "StoreManager", 5 }
            };

            // Find the highest priority role
            string highestRole = null;
            int highestPriority = int.MaxValue;

            foreach (var role in roles)
            {
                if (rolePriority.ContainsKey(role) && rolePriority[role] < highestPriority)
                {
                    highestRole = role;
                    highestPriority = rolePriority[role];
                }
            }

            // Return the highest priority role, or Pro Player if they have a player profile, or Member as default
            if (highestRole != null)
            {
                return highestRole;
            }
            else if (hasPlayerProfile)
            {
                return "Pro Player";
            }
            else
            {
                return "Member";
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new EditProfileViewModel
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NationalityId = user.NationalityId,
                Nationalities = await _context.Nationalities.ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Nationalities = await _context.Nationalities.ToListAsync();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Check if username is already taken by another user
            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Username), "Username is already taken.");
                model.Nationalities = await _context.Nationalities.ToListAsync();
                return View(model);
            }

            // Check if email is already taken by another user
            existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already taken.");
                model.Nationalities = await _context.Nationalities.ToListAsync();
                return View(model);
            }

            // Check if nationality exists
            if (!await _context.Nationalities.AnyAsync(n => n.Id == model.NationalityId))
            {
                ModelState.AddModelError(nameof(model.NationalityId), "Invalid nationality selected.");
                model.Nationalities = await _context.Nationalities.ToListAsync();
                return View(model);
            }

            // Update user properties
            user.UserName = model.Username;
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.NationalityId = model.NationalityId;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.Nationalities = await _context.Nationalities.ToListAsync();
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult UploadProfilePicture()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(UploadProfilePictureViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "Accounts");
                Directory.CreateDirectory(uploadsFolder);

                var ext = Path.GetExtension(model.ProfilePicture.FileName);
                var finalFileName = $"{Guid.NewGuid()}{ext}";
                var finalPath = Path.Combine(uploadsFolder, finalFileName);

                // write to temp first
                var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);

                try
                {
                    Console.WriteLine($">>> Writing upload to temp: {tempFile}");
                    await using (var tempStream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await model.ProfilePicture.CopyToAsync(tempStream);
                        await tempStream.FlushAsync();
                    }

                    // move to final destination atomically
                    Console.WriteLine($">>> Moving temp file to final path: {finalPath}");
                    if (System.IO.File.Exists(finalPath))
                    {
                        Console.WriteLine($">>> Final path already exists, deleting: {finalPath}");
                        System.IO.File.Delete(finalPath);
                    }
                    System.IO.File.Move(tempFile, finalPath);

                    // Delete old profile picture if it exists
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl.StartsWith("/uploads/Accounts/"))
                    {
                        var oldFilePath = Path.Combine(_env.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    user.ProfilePictureUrl = $"/uploads/Accounts/{finalFileName}";
                    await _userManager.UpdateAsync(user);

                    TempData["SuccessMessage"] = "Profile picture updated successfully!";
                    return RedirectToAction(nameof(Profile));
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine("❌ IO Exception while saving file: " + ioEx);
                    try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch { }
                    ModelState.AddModelError("", "File write error: " + ioEx.Message);
                    return View(model);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ General Exception while saving file: " + ex);
                    try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch { }
                    ModelState.AddModelError("", "Unexpected error while saving file.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SearchUsers(SearchUsersViewModel model)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                query = query.Where(u => 
                    u.UserName.Contains(model.SearchTerm) ||
                    u.FirstName.Contains(model.SearchTerm) ||
                    u.LastName.Contains(model.SearchTerm) ||
                    u.Email.Contains(model.SearchTerm));
            }

            var totalUsers = await query.CountAsync();

            var users = await query
                .Include(u => u.Nationality)
                .Include(u => u.Player)
                .Skip((model.CurrentPage - 1) * SearchUsersViewModel.UsersPerPage)
                .Take(SearchUsersViewModel.UsersPerPage)
                .ToListAsync();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userViewModels = new List<UserProfileViewModel>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var status = DetermineUserStatus(userRoles, user.PlayerId.HasValue);

                userViewModels.Add(new UserProfileViewModel
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    NationalityId = user.NationalityId,
                    Nationality = user.Nationality,
                    PlayerId = user.PlayerId,
                    Player = user.Player,
                    IsCurrentUser = user.Id == currentUserId,
                    UserStatus = status,
                    UserRoles = userRoles
                });
            }

            model.TotalUsers = totalUsers;
            model.Users = userViewModels;

            return View(model);
        }
    }
}
