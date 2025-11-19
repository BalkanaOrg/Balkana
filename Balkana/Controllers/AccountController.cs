using Balkana.Data.Models;
using Balkana.Models.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Balkana.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http;

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

        [HttpGet]
        public IActionResult GoogleLogin(string returnUrl = null)
        {
            // Set the redirect URI to our controller action
            // The OAuth middleware will process the callback at /signin-google
            // Then redirect to this URL, which is our controller action
            var callbackUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl }, Request.Scheme, Request.Host.Value);
            
            // Force HTTPS if needed
            if (!callbackUrl.StartsWith("https://"))
            {
                var uri = new UriBuilder(callbackUrl) { Scheme = "https", Port = 4444 };
                callbackUrl = uri.ToString();
            }
            
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", callbackUrl);
            
            // Store returnUrl in the properties so we can retrieve it in the callback
            if (!string.IsNullOrEmpty(returnUrl))
            {
                properties.Items["returnUrl"] = returnUrl;
            }
            
            return Challenge(properties, "Google");
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string returnUrl = null, string remoteError = null)
        {
            try
            {
                // Log for debugging
                Console.WriteLine($"GoogleCallback called - returnUrl: {returnUrl}, remoteError: {remoteError}");
                Console.WriteLine($"Request QueryString: {Request.QueryString}");
                
                if (remoteError != null)
                {
                    TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
                    return RedirectToAction(nameof(Login));
                }

                // Get external login info - this consumes the OAuth cookie
                var info = await _signInManager.GetExternalLoginInfoAsync();
                Console.WriteLine($"ExternalLoginInfo is null: {info == null}");
                
                if (info == null)
                {
                    Console.WriteLine("ERROR: ExternalLoginInfo is null - OAuth cookie may have been consumed already");
                    Console.WriteLine("This usually means the callback was called twice or the OAuth flow failed");
                    
                    TempData["ErrorMessage"] = "Error loading external login information. Please try signing in again.";
                    return RedirectToAction(nameof(Login));
                }
                
                Console.WriteLine($"ExternalLoginInfo retrieved - Provider: {info.LoginProvider}, Key: {info.ProviderKey}");
                
                // Note: ExternalLoginInfo doesn't have a Properties property
                // The returnUrl should come from the query string or route parameter
                // If it's not provided, we'll use null and redirect to home

                // Sign in the user with this external login provider if the user already has a login
                var signInResult = await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

                if (signInResult.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }

                // If the user does not have an account, then we need to create one
                if (signInResult.IsLockedOut)
                {
                    TempData["ErrorMessage"] = "Your account has been locked out.";
                    return RedirectToAction(nameof(Login));
                }

                // Get the email claim value
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? 
                               info.Principal.FindFirstValue(ClaimTypes.Name)?.Split(' ').FirstOrDefault() ?? "";
                var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? 
                              info.Principal.FindFirstValue(ClaimTypes.Name)?.Split(' ').Skip(1).FirstOrDefault() ?? "";
                var picture = info.Principal.FindFirstValue("urn:google:picture") ?? 
                             info.Principal.FindFirstValue("picture") ?? "";

                // Check if user already exists by email
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    // User exists but doesn't have Google login linked, add it
                    var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(existingUser, isPersistent: false);
                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Unable to link Google account. Please try again.";
                        return RedirectToAction(nameof(Login));
                    }
                }

                // User doesn't exist, redirect to complete registration
                var model = new GoogleRegisterViewModel
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    GoogleId = info.ProviderKey,
                    GoogleEmail = email,
                    GoogleFirstName = firstName,
                    GoogleLastName = lastName,
                    GoogleProfilePicture = picture,
                    Username = email?.Split('@')[0] ?? "user" // Default username from email
                };

                model.Nationalities = await _context.Nationalities.ToListAsync();
                return View("GoogleRegister", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GoogleCallback: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred during Google authentication. Please try again.";
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GoogleRegister()
        {
            // Try to get external login info from the authentication cookie
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "External login information expired. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? 
                           info.Principal.FindFirstValue(ClaimTypes.Name)?.Split(' ').FirstOrDefault() ?? "";
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? 
                          info.Principal.FindFirstValue(ClaimTypes.Name)?.Split(' ').Skip(1).FirstOrDefault() ?? "";
            var picture = info.Principal.FindFirstValue("urn:google:picture") ?? 
                         info.Principal.FindFirstValue("picture") ?? "";

            var model = new GoogleRegisterViewModel
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                GoogleId = info.ProviderKey,
                GoogleEmail = email,
                GoogleFirstName = firstName,
                GoogleLastName = lastName,
                GoogleProfilePicture = picture,
                Username = email?.Split('@')[0] ?? "user",
                Nationalities = await _context.Nationalities.ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoogleRegister(GoogleRegisterViewModel model)
        {
            Console.WriteLine($"GoogleRegister POST - Username: {model.Username}, Email: {model.Email}, NationalityId: {model.NationalityId}");
            Console.WriteLine($"GoogleProfilePicture: '{model.GoogleProfilePicture}'");
            
            // Remove validation errors for fields that shouldn't be validated
            ModelState.Remove(nameof(model.Nationalities));
            ModelState.Remove(nameof(model.GoogleProfilePicture));
            ModelState.Remove(nameof(model.GoogleId));
            ModelState.Remove(nameof(model.GoogleEmail));
            ModelState.Remove(nameof(model.GoogleFirstName));
            ModelState.Remove(nameof(model.GoogleLastName));
            ModelState.Remove(nameof(model.ProfilePicture)); // File upload not used for Google registration
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid. Errors:");
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Any())
                    {
                        Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }
                
                // Ensure GoogleProfilePicture is preserved
                if (string.IsNullOrEmpty(model.GoogleProfilePicture))
                {
                    // Try to get it from external login info if available
                    var externalInfo = await _signInManager.GetExternalLoginInfoAsync();
                    if (externalInfo != null)
                    {
                        var picture = externalInfo.Principal.FindFirstValue("urn:google:picture") ?? 
                                     externalInfo.Principal.FindFirstValue("picture") ?? "";
                        model.GoogleProfilePicture = picture;
                    }
                }
                
                model.Nationalities = await _context.Nationalities.ToListAsync();
                return View(model);
            }

            // Get external login info
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "External login information expired. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            // Check if username is already taken
            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Username), "Username is already taken.");
                model.Nationalities = await _context.Nationalities.ToListAsync();
                return View(model);
            }

            // Check if email is already taken
            existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
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

            // Download and save Google profile picture, or use default
            string profilePictureUrl;
            if (!string.IsNullOrEmpty(model.GoogleProfilePicture))
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(10);
                        var imageBytes = await httpClient.GetByteArrayAsync(model.GoogleProfilePicture);
                        
                        // Determine file extension from content type or URL
                        string extension = ".jpg"; // default
                        var uri = new Uri(model.GoogleProfilePicture);
                        var path = uri.AbsolutePath.ToLower();
                        if (path.EndsWith(".png"))
                            extension = ".png";
                        else if (path.EndsWith(".gif"))
                            extension = ".gif";
                        else if (path.EndsWith(".webp"))
                            extension = ".webp";
                        
                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "Accounts");
                        
                        // Ensure directory exists
                        if (!Directory.Exists(uploadsPath))
                        {
                            Directory.CreateDirectory(uploadsPath);
                        }
                        
                        var filePath = Path.Combine(uploadsPath, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                        
                        profilePictureUrl = $"/uploads/Accounts/{fileName}";
                        Console.WriteLine($"Downloaded and saved Google profile picture to: {profilePictureUrl}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading Google profile picture: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Use default if download fails
                    profilePictureUrl = "/uploads/Accounts/_default.png";
                    Console.WriteLine($"Using default profile picture: {profilePictureUrl}");
                }
            }
            else
            {
                // No Google profile picture, use default
                profilePictureUrl = "/uploads/Accounts/_default.png";
                Console.WriteLine($"No Google profile picture, using default: {profilePictureUrl}");
            }
            
            Console.WriteLine($"Creating user with profile picture: '{profilePictureUrl}'");
            
            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                NationalityId = model.NationalityId,
                ProfilePictureUrl = profilePictureUrl
            };

            // Create user without password (Google OAuth users don't need passwords)
            // We'll use a random secure password that the user will never know
            var randomPassword = Guid.NewGuid().ToString() + "!@#$%^&*()" + Guid.NewGuid().ToString();
            var result = await _userManager.CreateAsync(user, randomPassword);

            Console.WriteLine($"User creation result - Succeeded: {result.Succeeded}");
            if (!result.Succeeded)
            {
                Console.WriteLine("User creation errors:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  {error.Code}: {error.Description}");
                }
            }

            if (result.Succeeded)
            {
                Console.WriteLine($"User created successfully - ID: {user.Id}, Username: {user.UserName}");
                
                // Add default role
                var roleResult = await _userManager.AddToRoleAsync(user, "Member");
                Console.WriteLine($"Add to role 'Member' - Succeeded: {roleResult.Succeeded}");

                // Add external login
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                Console.WriteLine($"Add external login - Succeeded: {addLoginResult.Succeeded}");
                
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    Console.WriteLine("User signed in successfully, redirecting to Home");
                    TempData["SuccessMessage"] = "Account created successfully with Google!";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // If adding login fails, delete the user and show error
                    Console.WriteLine("Failed to add external login, deleting user");
                    await _userManager.DeleteAsync(user);
                    foreach (var error in addLoginResult.Errors)
                    {
                        Console.WriteLine($"  AddLogin error: {error.Code}: {error.Description}");
                        ModelState.AddModelError("", error.Description);
                    }
                    model.Nationalities = await _context.Nationalities.ToListAsync();
                    return View(model);
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.Nationalities = await _context.Nationalities.ToListAsync();
            return View(model);
        }
    }
}
