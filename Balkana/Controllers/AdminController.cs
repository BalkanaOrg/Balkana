using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Admin;
using Balkana.Services.Admin;
using Balkana.Services.Players;
using Balkana.Services.Players.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Balkana.Data.Infrastructure.Extensions;

namespace Balkana.Controllers
{
    [Authorize(Roles = "Administrator,Moderator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public IActionResult Index() => View();

        // List all users
        public IActionResult Users()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // Change role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (await _roleManager.RoleExistsAsync(role))
                await _userManager.AddToRoleAsync(user, role);

            return RedirectToAction("Users");
        }


        //ADD GAME PROFILE
        [HttpGet]
        public async Task<IActionResult> SearchPlayers(string term, int page = 1, int pageSize = 20)
        {
            var query = _context.Players.AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(p => p.Nickname.Contains(term));
            }

            var players = await query
                .OrderBy(p => p.Nickname)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.Id,
                    text = p.Nickname
                })
                .ToListAsync();

            return Json(new
            {
                results = players,
                pagination = new { more = players.Count == pageSize }
            });
        }

        // GET: show empty VM (no heavy player list)
        [HttpGet]
        [Route("admin/players/addgameprofile")]
        public async Task<IActionResult> AddGameProfile(int? playerId = null)
        {
            var vm = new AddGameProfileViewModel();

            if (playerId.HasValue)
            {
                var p = await _context.Players.FindAsync(playerId.Value);
                if (p != null)
                {
                    vm.SelectedPlayerId = p.Id;
                    vm.SelectedPlayerText = p.Nickname; // so Select2 pre-selects it
                }
            }

            return View("Players/AddGameProfile", vm);
        }


        // POST: handle creation with validation + duplicate prevention
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/players/addgameprofile")]
        public async Task<IActionResult> AddGameProfile(AddGameProfileViewModel model)
        {
            // extra check in case SelectedPlayerId is null/0
            if (!model.SelectedPlayerId.HasValue || model.SelectedPlayerId.Value == 0)
            {
                ModelState.AddModelError(nameof(model.SelectedPlayerId), "Please select a player.");
            }

            if (!ModelState.IsValid)
            {
                // If a player id was supplied, fetch its nickname so Select2 will show it as selected
                if (model.SelectedPlayerId.HasValue && model.SelectedPlayerId.Value != 0)
                {
                    var p = await _context.Players.FindAsync(model.SelectedPlayerId.Value);
                    if (p != null) model.SelectedPlayerText = p.Nickname;
                }

                // ensure Providers are present (they are normally set by the VM default, but keep defensive)
                model.Providers ??= new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "FACEIT", Text = "FACEIT" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "RIOT", Text = "RIOT" }
                };

                return View("Players/AddGameProfile", model);
            }

            var player = await _context.Players.FindAsync(model.SelectedPlayerId.Value);
            if (player == null) return NotFound();

            // Prevent duplicate (PlayerId + Provider)
            var already = await _context.Set<GameProfile>()
                .AnyAsync(g => g.PlayerId == player.Id && g.Provider == model.Provider);

            if (already)
            {
                ModelState.AddModelError(string.Empty, "This player already has a profile for that provider.");
                model.SelectedPlayerText = player.Nickname;
                model.Providers ??= new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "FACEIT", Text = "FACEIT" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "RIOT", Text = "RIOT" }
                };
                return View(model);
            }

            var profile = new GameProfile
            {
                PlayerId = player.Id,
                Provider = model.Provider,
                UUID = model.UUID
            };

            _context.GameProfiles.Add(profile);
            await _context.SaveChangesAsync();

            return RedirectToAction("Players", "Admin"); // or wherever you prefer
        }

        [HttpGet]
        [Route("admin/players/index")]
        public async Task<IActionResult> Players()
        {
            var players = await _context.Players
                .Include(p => p.GameProfiles)
                .OrderBy(p => p.Nickname)
                .Select(p => new PlayerWithProfilesViewModel
                {
                    PlayerId = p.Id,
                    Nickname = p.Nickname,
                    FullName = p.FirstName + " " + p.LastName,
                    GameProfiles = p.GameProfiles.Select(g => g.Provider).ToList()
                })
                .ToListAsync();

            var vm = new PlayersListViewModel { Players = players };

            return View("Players/Index", vm);
        }

        [HttpGet]
        public async Task<IActionResult> LookupFaceit(string nickname, [FromServices] IExternalApiService api)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                return BadRequest("Nickname required");

            var players = await api.SearchFaceitPlayersAsync(nickname);
            if (players == null || !players.Any())
                return NotFound("No Faceit players found");

            return Json(players);
        }

        [HttpGet]
        public async Task<IActionResult> LookupRiot(string gameName, string tagLine, string region, [FromServices] IExternalApiService api)
        {
            if (string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(tagLine) || string.IsNullOrWhiteSpace(region))
                return BadRequest("Game name, tagline and region are required");

            var uuid = await api.SearchRiotPlayerAsync(gameName, tagLine, region);
            if (uuid == null) return NotFound("No Riot player found");

            return Json(new { uuid });
        }

        [HttpGet]
        [Route("admin/players/add")]
        public IActionResult AddPlayer([FromServices] IPlayerService players)
        {
            return View("Players/Add", new PlayerFormModel
            {
                Nationalities = players.GetNationalities()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/players/add")]
        public IActionResult AddPlayer(
            PlayerFormModel player,
            [FromServices] IPlayerService players,
            [FromServices] ApplicationDbContext data)
        {
            if (!data.Nationalities.Any(c => c.Id == player.NationalityId))
            {
                ModelState.AddModelError(nameof(player.NationalityId), "This nationality isn't registered in the Database.");
            }

            if (!ModelState.IsValid)
            {
                player.Nationalities = players.GetNationalities();
                return View("Players/Add", player);
            }

            var playerId = players.Create(
                player.Nickname,
                player.FirstName,
                player.LastName,
                player.NationalityId);

            var pplayer = players.Profile(playerId);
            string playerInformation = pplayer.GetInformation();

            return RedirectToAction("Profile", "Players", new { id = playerId, information = playerInformation });
        }

        [HttpGet]
        [Route("admin/faceit/index")]
        public async Task<IActionResult> FaceitClubs()
        {
            var clubs = await _context.FaceitClubs.ToListAsync();
            var vm = new FaceitClubsViewModel { Clubs = clubs };
            return View("Faceit/Index", vm);
        }

        // GET: /Admin/Faceit/Add
        [HttpGet]
        [Route("admin/faceit/add")]
        public IActionResult AddFaceitClub()
        {
            return View("Faceit/Add", new FaceitClub());
        }

        // POST: /Admin/Faceit/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/faceit/add")]
        public async Task<IActionResult> AddFaceitClub(FaceitClub model)
        {
            if (!ModelState.IsValid)
            {
                return View("Faceit/Add", model);
            }

            _context.FaceitClubs.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Admin"); // or maybe redirect to a list of clubs later
        }
    }
}
