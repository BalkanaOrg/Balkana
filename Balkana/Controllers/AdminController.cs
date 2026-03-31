using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Data.Models.Store;
using Balkana.Models.Admin;
using Balkana.Models.Discord;
using Balkana.Models.Store;
using Balkana.Models.Tournaments;
using Balkana.Services.Admin;
using Balkana.Services.Discord;
using Balkana.Services.Players;
using Balkana.Services.Players.Models;
using Balkana.Services.Store;
using Balkana.Services.Tournaments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Balkana.Data.Infrastructure.Extensions;
using System.Security.Claims;

namespace Balkana.Controllers
{
    [Authorize(Roles = "Administrator,Moderator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index() => View();

        // List all users
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Nationality)
                .Include(u => u.Player)
                .ToListAsync();
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

        // Link/Unlink player to user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkPlayer(string userId, int? playerId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Check if player exists (if linking)
            if (playerId.HasValue)
            {
                var player = await _context.Players.FindAsync(playerId.Value);
                if (player == null) return NotFound();

                // Check if player is already linked to another user
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.PlayerId == playerId.Value && u.Id != userId);
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = $"Player {player.Nickname} is already linked to user {existingUser.UserName}.";
                    return RedirectToAction("Users");
                }
            }

            user.PlayerId = playerId;
            await _userManager.UpdateAsync(user);

            var message = playerId.HasValue ? "Player linked successfully!" : "Player unlinked successfully!";
            TempData["SuccessMessage"] = message;

            return RedirectToAction("Users");
        }

        // Search players for linking
        [HttpGet]
        public async Task<IActionResult> SearchPlayersForLinking(string term, int page = 1, int pageSize = 20)
        {
            var query = _context.Players.AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(p => 
                    p.Nickname.Contains(term) ||
                    p.FirstName.Contains(term) ||
                    p.LastName.Contains(term));
            }

            var players = await query
                .OrderBy(p => p.Nickname)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.Id,
                    text = p.Nickname,
                    fullName = p.FirstName + " " + p.LastName
                })
                .ToListAsync();

            return Json(new
            {
                results = players,
                pagination = new { more = players.Count == pageSize }
            });
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

            var normalizedUuid = model.UUID.Trim();
            var samePlayerDuplicate = await _context.Set<GameProfile>()
                .AnyAsync(g => g.PlayerId == player.Id && g.Provider == model.Provider && g.UUID == normalizedUuid);

            if (samePlayerDuplicate)
            {
                ModelState.AddModelError(string.Empty, "This player already has this UUID for that provider.");
                model.SelectedPlayerText = player.Nickname;
                model.Providers ??= new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "FACEIT", Text = "FACEIT" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "RIOT", Text = "RIOT" }
                };
                return View(model);
            }

            var uuidOwnedElsewhere = await _context.Set<GameProfile>()
                .AnyAsync(g => g.PlayerId != player.Id && g.Provider == model.Provider && g.UUID == normalizedUuid);

            if (uuidOwnedElsewhere)
            {
                ModelState.AddModelError(string.Empty, "Another player is already linked to this UUID for that provider.");
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
                UUID = normalizedUuid,
                DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? null : model.DisplayName.Trim()
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
                .ToListAsync();

            var rows = players.Select(p => new PlayerWithProfilesViewModel
            {
                PlayerId = p.Id,
                Nickname = p.Nickname,
                FullName = $"{p.FirstName} {p.LastName}",
                ProfileRows = p.GameProfiles.Select(g => new GameProfileAdminRow
                {
                    Provider = g.Provider,
                    DisplayName = g.DisplayName,
                    UuidShort = g.UUID.Length >= 8 ? g.UUID[..8] : g.UUID
                }).ToList()
            }).ToList();

            var vm = new PlayersListViewModel { Players = rows };

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

        // Discord Bot Management
        public IActionResult Discord()
        {
            return View("Discord/Index");
        }

        public IActionResult DiscordCommands()
        {
            var commands = new List<Balkana.Models.Discord.DiscordCommandViewModel>
            {
                new Balkana.Models.Discord.DiscordCommandViewModel
                {
                    Command = "/team",
                    Description = "Get active and benched players for a team",
                    Usage = "/team <team_tag_or_name>",
                    Example = "/team TDI or /team Team Diamond"
                },
                new Balkana.Models.Discord.DiscordCommandViewModel
                {
                    Command = "/player",
                    Description = "Get basic information for a player",
                    Usage = "/player <nickname>",
                    Example = "/player ext1nct"
                },
                new Balkana.Models.Discord.DiscordCommandViewModel
                {
                    Command = "/transfers",
                    Description = "Get transfer history for a player",
                    Usage = "/transfers <nickname>",
                    Example = "/transfers ext1nct"
                },
                new Balkana.Models.Discord.DiscordCommandViewModel
                {
                    Command = "/bracket",
                    Description = "Get bracket image for a tournament",
                    Usage = "/bracket <tournament_name>",
                    Example = "/bracket CS2 Spring Championship"
                }
            };

            return View("Discord/Commands", commands);
        }

        public IActionResult DiscordTest()
        {
            return View("Discord/TestCommand");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DiscordTest(string command, string arguments, [FromServices] IDiscordBotService discordBotService)
        {
            try
            {
                ViewBag.Command = command;
                ViewBag.Arguments = arguments;

                if (string.IsNullOrEmpty(command))
                {
                    ViewBag.Response = new DiscordCommandResponse
                    {
                        Success = false,
                        Message = "Please select a command."
                    };
                    return View("Discord/TestCommand");
                }

                var argsArray = string.IsNullOrEmpty(arguments) 
                    ? Array.Empty<string>() 
                    : arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var response = await discordBotService.ProcessCommandAsync(command, argsArray);

                ViewBag.Response = new DiscordCommandResponse
                {
                    Success = true,
                    Message = response
                };
            }
            catch (Exception ex)
            {
                ViewBag.Response = new DiscordCommandResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }

            return View("Discord/TestCommand");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDiscordCommands([FromServices] IDiscordBotService discordBotService)
        {
            try
            {
                var success = await discordBotService.RegisterSlashCommandsAsync();
                
                if (success)
                {
                    return Json(new { success = true, message = "Commands registered successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to register commands with Discord API." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public IActionResult DiscordDiagnostic()
        {
            var user = User;
            var isAuthenticated = user.Identity?.IsAuthenticated ?? false;
            var userName = user.Identity?.Name ?? "Unknown";
            var roles = user.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            
            ViewBag.IsAuthenticated = isAuthenticated;
            ViewBag.UserName = userName;
            ViewBag.Roles = roles;
            ViewBag.IsAdmin = roles.Contains("Administrator");
            ViewBag.IsModerator = roles.Contains("Moderator");
            
            return View("Discord/Diagnostic");
        }

        public async Task<IActionResult> DiscordResultChannels()
        {
            var rows = await _context.DiscordGameResultChannels
                .AsNoTracking()
                .Include(x => x.Game)
                .OrderBy(x => x.Game.FullName)
                .ToListAsync();
            return View("Discord/ResultChannels", rows);
        }

        public async Task<IActionResult> DiscordResultChannelCreate()
        {
            await LoadGamesSelectListAsync();
            return View("Discord/ResultChannelForm", new DiscordGameResultChannel { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DiscordResultChannelCreate(
            [Bind(nameof(DiscordGameResultChannel.GameId), nameof(DiscordGameResultChannel.DiscordChannelId), nameof(DiscordGameResultChannel.DisplayLabel), nameof(DiscordGameResultChannel.IsActive))]
            DiscordGameResultChannel model)
        {
            if (model.GameId <= 0)
                ModelState.AddModelError(nameof(model.GameId), "Select a game.");
            else if (!await _context.Games.AnyAsync(g => g.Id == model.GameId))
                ModelState.AddModelError(nameof(model.GameId), "The selected game no longer exists. Refresh the page and try again.");

            if (string.IsNullOrWhiteSpace(model.DiscordChannelId) || !IsDiscordSnowflake(model.DiscordChannelId.Trim()))
                ModelState.AddModelError(nameof(model.DiscordChannelId), "Enter a valid numeric Discord channel snowflake (copy from Discord).");

            if (model.GameId > 0 && await _context.DiscordGameResultChannels.AnyAsync(x => x.GameId == model.GameId))
                ModelState.AddModelError(nameof(model.GameId), "This game already has a channel mapping. Edit or delete it first.");

            if (!ModelState.IsValid)
            {
                await LoadGamesSelectListAsync();
                return View("Discord/ResultChannelForm", model);
            }

            model.DiscordChannelId = model.DiscordChannelId.Trim();
            model.Game = null;
            _context.DiscordGameResultChannels.Add(model);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "DiscordGameResultChannel create failed");
                ModelState.AddModelError(string.Empty, "Could not save (duplicate game, invalid game id, or database error). Check that the game exists and is not already mapped.");
                await LoadGamesSelectListAsync();
                return View("Discord/ResultChannelForm", model);
            }

            TempData["Success"] = "Discord result channel saved.";
            return RedirectToAction(nameof(DiscordResultChannels));
        }

        public async Task<IActionResult> DiscordResultChannelEdit(int id)
        {
            var row = await _context.DiscordGameResultChannels.FindAsync(id);
            if (row == null)
                return NotFound();
            await LoadGamesSelectListAsync();
            return View("Discord/ResultChannelForm", row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DiscordResultChannelEdit(
            int id,
            [Bind(nameof(DiscordGameResultChannel.Id), nameof(DiscordGameResultChannel.GameId), nameof(DiscordGameResultChannel.DiscordChannelId), nameof(DiscordGameResultChannel.DisplayLabel), nameof(DiscordGameResultChannel.IsActive))]
            DiscordGameResultChannel model)
        {
            if (id != model.Id)
                return BadRequest();

            if (model.GameId <= 0)
                ModelState.AddModelError(nameof(model.GameId), "Select a game.");
            else if (!await _context.Games.AnyAsync(g => g.Id == model.GameId))
                ModelState.AddModelError(nameof(model.GameId), "The selected game no longer exists. Refresh the page and try again.");

            if (string.IsNullOrWhiteSpace(model.DiscordChannelId) || !IsDiscordSnowflake(model.DiscordChannelId.Trim()))
                ModelState.AddModelError(nameof(model.DiscordChannelId), "Enter a valid numeric Discord channel snowflake (copy from Discord).");

            if (model.GameId > 0 && await _context.DiscordGameResultChannels.AnyAsync(x => x.GameId == model.GameId && x.Id != id))
                ModelState.AddModelError(nameof(model.GameId), "Another row already uses this game.");

            if (!ModelState.IsValid)
            {
                await LoadGamesSelectListAsync();
                return View("Discord/ResultChannelForm", model);
            }

            var row = await _context.DiscordGameResultChannels.FindAsync(id);
            if (row == null)
                return NotFound();

            row.GameId = model.GameId;
            row.DiscordChannelId = model.DiscordChannelId.Trim();
            row.DisplayLabel = model.DisplayLabel;
            row.IsActive = model.IsActive;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "DiscordGameResultChannel edit failed for id {Id}", id);
                ModelState.AddModelError(string.Empty, "Could not save (duplicate game, invalid game id, or database error).");
                await LoadGamesSelectListAsync();
                return View("Discord/ResultChannelForm", model);
            }

            TempData["Success"] = "Mapping updated.";
            return RedirectToAction(nameof(DiscordResultChannels));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DiscordResultChannelDelete(int id)
        {
            var row = await _context.DiscordGameResultChannels.FindAsync(id);
            if (row != null)
            {
                _context.DiscordGameResultChannels.Remove(row);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Mapping deleted.";
            }
            return RedirectToAction(nameof(DiscordResultChannels));
        }

        public async Task<IActionResult> DiscordTournamentResults()
        {
            var tournaments = await _context.Tournaments
                .AsNoTracking()
                .OrderByDescending(t => t.EndDate)
                .Take(300)
                .Select(t => new { t.Id, t.FullName })
                .ToListAsync();
            ViewBag.TournamentSelect = new SelectList(tournaments, "Id", "FullName");
            return View("Discord/TournamentResultsPost");
        }

        public async Task<IActionResult> DiscordTournamentResultsPreview(int id, [FromServices] IDiscordTournamentResultsService resultsService)
        {
            var preview = await resultsService.BuildPlainTextPreviewAsync(id);
            if (preview == null)
                return NotFound();
            ViewData["Title"] = "Tournament results preview";
            return View("Discord/TournamentResultsPreview", preview);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DiscordTournamentResults(
            int tournamentId,
            string? channelIdOverride,
            [FromServices] IDiscordTournamentResultsService resultsService)
        {
            var trimmed = string.IsNullOrWhiteSpace(channelIdOverride) ? null : channelIdOverride.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !IsDiscordSnowflake(trimmed))
            {
                TempData["Error"] = "Channel override must be a numeric Discord snowflake.";
                return RedirectToAction(nameof(DiscordTournamentResults));
            }

            var (ok, err) = await resultsService.PostTournamentResultsAsync(tournamentId, trimmed);
            if (ok)
                TempData["Success"] = "Results posted to Discord.";
            else
                TempData["Error"] = err ?? "Failed to post to Discord.";

            return RedirectToAction(nameof(DiscordTournamentResults));
        }

        private async Task LoadGamesSelectListAsync()
        {
            var games = await _context.Games.AsNoTracking().OrderBy(g => g.FullName).ToListAsync();
            ViewBag.GamesSelect = new SelectList(games, "Id", "FullName");
        }

        /// <summary>Discord snowflakes are 64-bit unsigned integers (typically 17–20 decimal digits).</summary>
        private static bool IsDiscordSnowflake(string s) =>
            ulong.TryParse(s, System.Globalization.NumberStyles.None, null, out var v) && v > 0UL;

        // ============================================
        // BALKANA AWARDS
        // ============================================

        [HttpGet]
        [Route("admin/balkana-awards")]
        public IActionResult BalkanaAwards()
        {
            return View("BalkanaAwards/Index");
        }

        [HttpGet]
        [Route("admin/balkana-awards/poll/{year?}")]
        public IActionResult BalkanaAwardsPoll(int? year = null)
        {
            ViewBag.Year = year ?? DateTime.UtcNow.Year;
            return View("BalkanaAwards/Poll");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/balkana-awards/{year:int}/finalize-poty")]
        public async Task<IActionResult> BalkanaAwardsFinalizePoty(int year)
        {
            var evt = await _context.BalkanaAwards.FirstOrDefaultAsync(e => e.Year == year);
            if (evt == null)
            {
                evt = new BalkanaAwardsEvent
                {
                    Year = year,
                    EventDate = new DateTime(year, 12, 31, 0, 0, 0, DateTimeKind.Utc)
                };
                _context.BalkanaAwards.Add(evt);
                await _context.SaveChangesAsync();
            }

            var categories = await _context.BalkanaAwardCategories
                .AsNoTracking()
                .Where(c => c.Key == "cs_poty" || c.Key == "lol_poty")
                .ToListAsync();

            var csCat = categories.FirstOrDefault(c => c.Key == "cs_poty");
            var lolCat = categories.FirstOrDefault(c => c.Key == "lol_poty");

            if (csCat == null || lolCat == null)
            {
                TempData["Error"] = "Missing Balkana award categories (cs_poty / lol_poty). Ensure the SQL migration seeded categories.";
                return RedirectToAction(nameof(BalkanaAwardsPoll), new { year });
            }

            var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            async Task<List<int>> ComputeCsTop10Async()
            {
                var csStats = await _context.PlayerStatsCS
                    .AsNoTracking()
                    .Where(ps => ps.Match.PlayedAt >= start && ps.Match.PlayedAt < end)
                    .Join(_context.GameProfiles.AsNoTracking(),
                        ps => ps.PlayerUUID,
                        gp => gp.UUID,
                        (ps, gp) => new { ps, gp })
                    .Where(x => x.gp.Provider == x.ps.Source)
                    .GroupBy(x => x.gp.PlayerId)
                    .Select(g => new
                    {
                        PlayerId = g.Key,
                        Matches = g.Count(),
                        AvgHltv2 = g.Average(x => x.ps.HLTV2)
                    })
                    .Where(x => x.Matches >= 5)
                    .OrderByDescending(x => x.AvgHltv2)
                    .ThenByDescending(x => x.Matches)
                    .Take(10)
                    .Select(x => x.PlayerId)
                    .ToListAsync();

                return csStats;
            }

            async Task<List<int>> ComputeLolTop10Async()
            {
                var lolStats = await _context.PlayerStatsLoL
                    .AsNoTracking()
                    .Where(ps => ps.Match.PlayedAt >= start && ps.Match.PlayedAt < end)
                    .Join(_context.GameProfiles.AsNoTracking(),
                        ps => ps.PlayerUUID,
                        gp => gp.UUID,
                        (ps, gp) => new { ps, gp })
                    .Where(x => x.gp.Provider == x.ps.Source)
                    .GroupBy(x => x.gp.PlayerId)
                    .Select(g => new
                    {
                        PlayerId = g.Key,
                        Matches = g.Count(),
                        AvgKda = g.Average(x =>
                            ((double)(x.ps.Kills ?? 0) + (double)(x.ps.Assists ?? 0)) / Math.Max(1.0, (double)(x.ps.Deaths ?? 0)))
                    })
                    .Where(x => x.Matches >= 5)
                    .OrderByDescending(x => x.AvgKda)
                    .ThenByDescending(x => x.Matches)
                    .Take(10)
                    .Select(x => x.PlayerId)
                    .ToListAsync();

                return lolStats;
            }

            var csTop10 = await ComputeCsTop10Async();
            var lolTop10 = await ComputeLolTop10Async();

            var existing = await _context.BalkanaAwardResults
                .Where(r => r.BalkanaAwardsId == evt.Id && (r.CategoryId == csCat.Id || r.CategoryId == lolCat.Id))
                .ToListAsync();
            if (existing.Count > 0)
                _context.BalkanaAwardResults.RemoveRange(existing);

            for (int i = 0; i < csTop10.Count; i++)
            {
                _context.BalkanaAwardResults.Add(new BalkanaAwardResult
                {
                    BalkanaAwardsId = evt.Id,
                    CategoryId = csCat.Id,
                    Rank = i + 1,
                    PlayerId = csTop10[i]
                });
            }

            for (int i = 0; i < lolTop10.Count; i++)
            {
                _context.BalkanaAwardResults.Add(new BalkanaAwardResult
                {
                    BalkanaAwardsId = evt.Id,
                    CategoryId = lolCat.Id,
                    Rank = i + 1,
                    PlayerId = lolTop10[i]
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "POTY top10 results have been finalized for the selected year.";
            return RedirectToAction(nameof(BalkanaAwardsPoll), new { year });
        }

        [HttpGet]
        [Route("admin/balkana-awards/manual/{year?}")]
        public async Task<IActionResult> BalkanaAwardsManual(int? year = null)
        {
            var y = year ?? DateTime.UtcNow.Year;
            ViewBag.Year = y;

            var evt = await _context.BalkanaAwards.FirstOrDefaultAsync(e => e.Year == y);
            if (evt == null)
            {
                evt = new BalkanaAwardsEvent
                {
                    Year = y,
                    EventDate = new DateTime(y, 12, 31, 0, 0, 0, DateTimeKind.Utc)
                };
                _context.BalkanaAwards.Add(evt);
                await _context.SaveChangesAsync();
            }

            var vm = new Balkana.Models.Admin.BalkanaAwardsManualAwardViewModel
            {
                Year = y,
                AwardDate = evt.EventDate
            };

            return View("BalkanaAwards/Manual", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/balkana-awards/manual/{year:int}")]
        public async Task<IActionResult> BalkanaAwardsManual(int year,
            Balkana.Models.Admin.BalkanaAwardsManualAwardViewModel model,
            [FromServices] IWebHostEnvironment env)
        {
            model.Year = year;

            var evt = await _context.BalkanaAwards.FirstOrDefaultAsync(e => e.Year == year);
            if (evt == null)
            {
                evt = new BalkanaAwardsEvent
                {
                    Year = year,
                    EventDate = model.AwardDate
                };
                _context.BalkanaAwards.Add(evt);
            }
            else
            {
                evt.EventDate = model.AwardDate;
            }

            bool NeedId(bool give, object? id, string field)
            {
                if (!give) return false;
                if (id == null || (id is int i && i <= 0) || (id is string s && string.IsNullOrWhiteSpace(s)))
                {
                    ModelState.AddModelError(field, "Required when the award is enabled.");
                    return true;
                }
                return false;
            }

            NeedId(model.GiveEntryFragger, model.EntryFraggerPlayerId, nameof(model.EntryFraggerPlayerId));
            NeedId(model.GiveAwper, model.AwperPlayerId, nameof(model.AwperPlayerId));
            NeedId(model.GiveIgl, model.IglPlayerId, nameof(model.IglPlayerId));
            NeedId(model.GiveTeamOfYear, model.TeamOfYearTeamId, nameof(model.TeamOfYearTeamId));
            NeedId(model.GiveTournamentOfYear, model.TournamentOfYearTournamentId, nameof(model.TournamentOfYearTournamentId));
            NeedId(model.GiveContentCreator, model.ContentCreatorUserId, nameof(model.ContentCreatorUserId));
            NeedId(model.GiveStreamer, model.StreamerUserId, nameof(model.StreamerUserId));
            NeedId(model.GivePlayByPlayCaster, model.PlayByPlayCasterUserId, nameof(model.PlayByPlayCasterUserId));
            NeedId(model.GiveColorCaster, model.ColorCasterUserId, nameof(model.ColorCasterUserId));

            // Manual POTY (CS + LoL), ranks 1..10
            bool GetGive(bool isCs, int rank) => (isCs, rank) switch
            {
                (true, 1) => model.GiveCsPoty1,
                (true, 2) => model.GiveCsPoty2,
                (true, 3) => model.GiveCsPoty3,
                (true, 4) => model.GiveCsPoty4,
                (true, 5) => model.GiveCsPoty5,
                (true, 6) => model.GiveCsPoty6,
                (true, 7) => model.GiveCsPoty7,
                (true, 8) => model.GiveCsPoty8,
                (true, 9) => model.GiveCsPoty9,
                (true, 10) => model.GiveCsPoty10,
                (false, 1) => model.GiveLolPoty1,
                (false, 2) => model.GiveLolPoty2,
                (false, 3) => model.GiveLolPoty3,
                (false, 4) => model.GiveLolPoty4,
                (false, 5) => model.GiveLolPoty5,
                (false, 6) => model.GiveLolPoty6,
                (false, 7) => model.GiveLolPoty7,
                (false, 8) => model.GiveLolPoty8,
                (false, 9) => model.GiveLolPoty9,
                (false, 10) => model.GiveLolPoty10,
                _ => false
            };

            int? GetPlayerId(bool isCs, int rank) => (isCs, rank) switch
            {
                (true, 1) => model.CsPotyPlayerId1,
                (true, 2) => model.CsPotyPlayerId2,
                (true, 3) => model.CsPotyPlayerId3,
                (true, 4) => model.CsPotyPlayerId4,
                (true, 5) => model.CsPotyPlayerId5,
                (true, 6) => model.CsPotyPlayerId6,
                (true, 7) => model.CsPotyPlayerId7,
                (true, 8) => model.CsPotyPlayerId8,
                (true, 9) => model.CsPotyPlayerId9,
                (true, 10) => model.CsPotyPlayerId10,
                (false, 1) => model.LolPotyPlayerId1,
                (false, 2) => model.LolPotyPlayerId2,
                (false, 3) => model.LolPotyPlayerId3,
                (false, 4) => model.LolPotyPlayerId4,
                (false, 5) => model.LolPotyPlayerId5,
                (false, 6) => model.LolPotyPlayerId6,
                (false, 7) => model.LolPotyPlayerId7,
                (false, 8) => model.LolPotyPlayerId8,
                (false, 9) => model.LolPotyPlayerId9,
                (false, 10) => model.LolPotyPlayerId10,
                _ => null
            };

            IFormFile? GetIcon(bool isCs, int rank) => (isCs, rank) switch
            {
                (true, 1) => model.CsPotyIcon1,
                (true, 2) => model.CsPotyIcon2,
                (true, 3) => model.CsPotyIcon3,
                (true, 4) => model.CsPotyIcon4,
                (true, 5) => model.CsPotyIcon5,
                (true, 6) => model.CsPotyIcon6,
                (true, 7) => model.CsPotyIcon7,
                (true, 8) => model.CsPotyIcon8,
                (true, 9) => model.CsPotyIcon9,
                (true, 10) => model.CsPotyIcon10,
                (false, 1) => model.LolPotyIcon1,
                (false, 2) => model.LolPotyIcon2,
                (false, 3) => model.LolPotyIcon3,
                (false, 4) => model.LolPotyIcon4,
                (false, 5) => model.LolPotyIcon5,
                (false, 6) => model.LolPotyIcon6,
                (false, 7) => model.LolPotyIcon7,
                (false, 8) => model.LolPotyIcon8,
                (false, 9) => model.LolPotyIcon9,
                (false, 10) => model.LolPotyIcon10,
                _ => null
            };

            for (int rank = 1; rank <= 10; rank++)
            {
                if (GetGive(isCs: true, rank))
                {
                    var pid = GetPlayerId(isCs: true, rank);
                    if (!pid.HasValue || pid.Value <= 0)
                        ModelState.AddModelError($"CsPotyPlayerId{rank}", "Required when this rank is enabled.");
                }

                if (GetGive(isCs: false, rank))
                {
                    var pid = GetPlayerId(isCs: false, rank);
                    if (!pid.HasValue || pid.Value <= 0)
                        ModelState.AddModelError($"LolPotyPlayerId{rank}", "Required when this rank is enabled.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Year = year;
                return View("BalkanaAwards/Manual", model);
            }

            string baseFolder = Path.Combine("uploads", "BalkanaAwards", "Trophies", year.ToString());
            async Task<string> SaveIconAsync(IFormFile? file, string fallback)
            {
                if (file == null || file.Length == 0) return fallback;
                return await Balkana.Services.Images.ImageOptimizer.SaveWebpAsync(
                    file,
                    env.WebRootPath,
                    baseFolder,
                    maxWidth: 1024,
                    maxHeight: 1024,
                    quality: 85);
            }

            const string awardType = "Balkana Awards";
            string defaultIcon = "/uploads/Tournaments/Trophies/default_trophy.png";

            async Task<int> CreateTrophyAsync(string name, string description, IFormFile? iconFile)
            {
                var iconUrl = await SaveIconAsync(iconFile, defaultIcon);
                var trophy = new Trophy
                {
                    Name = name,
                    Description = description,
                    IconURL = iconUrl,
                    AwardType = awardType,
                    AwardDate = model.AwardDate
                };
                _context.Trophies.Add(trophy);
                await _context.SaveChangesAsync();
                return trophy.Id;
            }

            // Manual POTY trophies (CS + LoL)
            for (int rank = 1; rank <= 10; rank++)
            {
                if (GetGive(isCs: true, rank))
                {
                    var pid = GetPlayerId(isCs: true, rank)!.Value;
                    var trophyId = await CreateTrophyAsync(
                        $"Counter-Strike Player of the Year #{rank}",
                        $"Balkana Awards {year} - Counter-Strike Player of the Year #{rank}",
                        GetIcon(isCs: true, rank));
                    _context.PlayerTrophies.Add(new PlayerTrophy { PlayerId = pid, TrophyId = trophyId, DateAwarded = model.AwardDate });
                }

                if (GetGive(isCs: false, rank))
                {
                    var pid = GetPlayerId(isCs: false, rank)!.Value;
                    var trophyId = await CreateTrophyAsync(
                        $"League of Legends Player of the Year #{rank}",
                        $"Balkana Awards {year} - League of Legends Player of the Year #{rank}",
                        GetIcon(isCs: false, rank));
                    _context.PlayerTrophies.Add(new PlayerTrophy { PlayerId = pid, TrophyId = trophyId, DateAwarded = model.AwardDate });
                }
            }

            if (model.GiveEntryFragger)
            {
                var trophyId = await CreateTrophyAsync("Entry Fragger of the Year", $"Balkana Awards {year} - Entry Fragger of the Year", model.EntryFraggerIcon);
                _context.PlayerTrophies.Add(new PlayerTrophy { PlayerId = model.EntryFraggerPlayerId!.Value, TrophyId = trophyId, DateAwarded = model.AwardDate });
            }

            if (model.GiveAwper)
            {
                var trophyId = await CreateTrophyAsync("AWPer of the Year", $"Balkana Awards {year} - AWPer of the Year", model.AwperIcon);
                _context.PlayerTrophies.Add(new PlayerTrophy { PlayerId = model.AwperPlayerId!.Value, TrophyId = trophyId, DateAwarded = model.AwardDate });
            }

            if (model.GiveIgl)
            {
                var trophyId = await CreateTrophyAsync("IGL of the Year", $"Balkana Awards {year} - IGL of the Year", model.IglIcon);
                _context.PlayerTrophies.Add(new PlayerTrophy { PlayerId = model.IglPlayerId!.Value, TrophyId = trophyId, DateAwarded = model.AwardDate });
            }

            if (model.GiveTeamOfYear)
            {
                var trophyId = await CreateTrophyAsync("Team of the Year", $"Balkana Awards {year} - Team of the Year", model.TeamOfYearIcon);
                _context.TeamTrophies.Add(new TeamTrophy { TeamId = model.TeamOfYearTeamId!.Value, TrophyId = trophyId });
            }

            if (model.GiveTournamentOfYear)
            {
                var iconUrl = await SaveIconAsync(model.TournamentOfYearIcon, defaultIcon);
                var trophy = new TrophyTournament
                {
                    Name = "Tournament of the Year",
                    TournamentId = model.TournamentOfYearTournamentId!.Value,
                    Description = $"Balkana Awards {year} - Tournament of the Year",
                    IconURL = iconUrl,
                    AwardType = awardType,
                    AwardDate = model.AwardDate
                };
                _context.Trophies.Add(trophy);
                await _context.SaveChangesAsync();
            }

            if (model.GiveContentCreator)
            {
                var trophyId = await CreateTrophyAsync("Content Creator of the Year", $"Balkana Awards {year} - Content Creator of the Year", model.ContentCreatorIcon);
                _context.UserTrophies.Add(new UserTrophy { UserId = model.ContentCreatorUserId!, TrophyId = trophyId, DateAwarded = model.AwardDate });
            }

            if (model.GiveStreamer)
            {
                var trophyId = await CreateTrophyAsync("Streamer of the Year", $"Balkana Awards {year} - Streamer of the Year", model.StreamerIcon);
                _context.UserTrophies.Add(new UserTrophy { UserId = model.StreamerUserId!, TrophyId = trophyId, DateAwarded = model.AwardDate });
            }

            if (model.GivePlayByPlayCaster)
            {
                var trophyId = await CreateTrophyAsync("Play-by-Play Caster of the Year", $"Balkana Awards {year} - Play-by-Play Caster of the Year", model.PlayByPlayCasterIcon);
                _context.UserTrophies.Add(new UserTrophy { UserId = model.PlayByPlayCasterUserId!, TrophyId = trophyId, DateAwarded = model.AwardDate });
            }

            if (model.GiveColorCaster)
            {
                var trophyId = await CreateTrophyAsync("Color Caster of the Year", $"Balkana Awards {year} - Color Caster of the Year", model.ColorCasterIcon);
                _context.UserTrophies.Add(new UserTrophy { UserId = model.ColorCasterUserId!, TrophyId = trophyId, DateAwarded = model.AwardDate });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Balkana Awards trophies issued.";
            return RedirectToAction(nameof(BalkanaAwardsManual), new { year });
        }

        [HttpGet]
        [Route("admin/balkana-awards/search/players")]
        public async Task<IActionResult> BalkanaAwardsSearchPlayers(string term, int page = 1, int pageSize = 20)
        {
            var query = _context.Players.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                query = query.Where(p =>
                    p.Nickname.Contains(t) ||
                    p.FirstName.Contains(t) ||
                    p.LastName.Contains(t));
            }

            var rows = await query
                .OrderBy(p => p.Nickname)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new { id = p.Id, text = p.Nickname })
                .ToListAsync();

            return Json(new
            {
                results = rows,
                pagination = new { more = rows.Count == pageSize }
            });
        }

        [HttpGet]
        [Route("admin/balkana-awards/search/teams")]
        public async Task<IActionResult> BalkanaAwardsSearchTeams(string term, int page = 1, int pageSize = 20)
        {
            var query = _context.Teams.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                query = query.Where(team => team.Tag.Contains(t) || team.FullName.Contains(t));
            }

            var rows = await query
                .OrderBy(team => team.Tag)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(team => new { id = team.Id, text = $"{team.Tag} - {team.FullName}" })
                .ToListAsync();

            return Json(new
            {
                results = rows,
                pagination = new { more = rows.Count == pageSize }
            });
        }

        [HttpGet]
        [Route("admin/balkana-awards/search/tournaments")]
        public async Task<IActionResult> BalkanaAwardsSearchTournaments(string term, int page = 1, int pageSize = 20)
        {
            var query = _context.Tournaments.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                query = query.Where(tr => tr.FullName.Contains(t));
            }

            var rows = await query
                .OrderByDescending(tr => tr.EndDate)
                .ThenBy(tr => tr.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(tr => new { id = tr.Id, text = tr.FullName })
                .ToListAsync();

            return Json(new
            {
                results = rows,
                pagination = new { more = rows.Count == pageSize }
            });
        }

        [HttpGet]
        [Route("admin/balkana-awards/search/users")]
        public async Task<IActionResult> BalkanaAwardsSearchUsers(string term, int page = 1, int pageSize = 20)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                query = query.Where(u =>
                    u.UserName.Contains(t) ||
                    u.Email.Contains(t) ||
                    u.FirstName.Contains(t) ||
                    u.LastName.Contains(t));
            }

            var rows = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new { id = u.Id, text = u.UserName })
                .ToListAsync();

            return Json(new
            {
                results = rows,
                pagination = new { more = rows.Count == pageSize }
            });
        }

        // ============================================
        // RIOT TOURNAMENT API MANAGEMENT
        // ============================================

        /// <summary>
        /// List all Riot Tournaments
        /// </summary>
        [HttpGet]
        [Route("admin/riot-tournaments")]
        public async Task<IActionResult> RiotTournaments([FromServices] IRiotTournamentService tournamentService)
        {
            var tournaments = await tournamentService.GetAllTournamentsAsync();

            var vm = new RiotTournamentListViewModel
            {
                Tournaments = tournaments.Select(t => new RiotTournamentItemViewModel
                {
                    Id = t.Id,
                    RiotTournamentId = t.RiotTournamentId,
                    Name = t.Name,
                    Region = t.Region,
                    ProviderId = t.ProviderId,
                    CreatedAt = t.CreatedAt,
                    TournamentId = t.TournamentId,
                    TournamentName = t.Tournament?.FullName,
                    TotalCodes = t.TournamentCodes.Count,
                    UsedCodes = t.TournamentCodes.Count(c => c.IsUsed),
                    UnusedCodes = t.TournamentCodes.Count(c => !c.IsUsed)
                }).ToList()
            };

            return View("RiotTournaments/Index", vm);
        }

        /// <summary>
        /// Show form to create a new Riot Tournament
        /// </summary>
        [HttpGet]
        [Route("admin/riot-tournaments/create")]
        public async Task<IActionResult> CreateRiotTournament()
        {
            var tournaments = await _context.Tournaments
                .Where(t => t.Game.FullName == "League of Legends")
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.FullName
                })
                .ToListAsync();

            var vm = new CreateRiotTournamentViewModel
            {
                InternalTournaments = tournaments
            };

            return View("RiotTournaments/Create", vm);
        }

        /// <summary>
        /// Create a new Riot Tournament
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/riot-tournaments/create")]
        public async Task<IActionResult> CreateRiotTournament(CreateRiotTournamentViewModel model, [FromServices] IRiotTournamentService tournamentService)
        {
            if (!ModelState.IsValid)
            {
                var tournaments = await _context.Tournaments
                    .Where(t => t.Game.FullName == "League of Legends")
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.FullName
                    })
                    .ToListAsync();

                model.InternalTournaments = tournaments;
                return View("RiotTournaments/Create", model);
            }

            try
            {
                // If no provider ID, register one (POST /providers - validates API key and creates provider)
                if (!model.ProviderId.HasValue)
                {
                    model.ProviderId = await tournamentService.RegisterProviderAsync(model.Region, callbackUrl: null);
                }

                var tournament = await tournamentService.CreateTournamentAsync(
                    model.Name,
                    model.ProviderId.Value,
                    model.Region,
                    model.TournamentId);

                TempData["SuccessMessage"] = $"Riot Tournament '{tournament.Name}' created successfully! Riot Tournament ID: {tournament.RiotTournamentId}";
                return RedirectToAction("RiotTournamentDetails", new { id = tournament.Id });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (msg.Contains("403") || msg.Contains("Forbidden"))
                    msg = "API key is invalid or lacks tournament-v5 permissions. Ensure your production key has tournament access and the correct region (americas/europe).";
                ModelState.AddModelError("", $"Error creating tournament: {msg}");
                
                var tournaments = await _context.Tournaments
                    .Where(t => t.Game.FullName == "League of Legends")
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.FullName
                    })
                    .ToListAsync();

                model.InternalTournaments = tournaments;
                return View("RiotTournaments/Create", model);
            }
        }

        /// <summary>
        /// Show details of a Riot Tournament including all codes
        /// </summary>
        [HttpGet]
        [Route("admin/riot-tournaments/{id}")]
        public async Task<IActionResult> RiotTournamentDetails(int id, [FromServices] IRiotTournamentService tournamentService)
        {
            var tournament = await tournamentService.GetTournamentByIdAsync(id);

            if (tournament == null)
                return NotFound();

            var vm = new RiotTournamentDetailsViewModel
            {
                Id = tournament.Id,
                RiotTournamentId = tournament.RiotTournamentId,
                Name = tournament.Name,
                Region = tournament.Region,
                ProviderId = tournament.ProviderId,
                CreatedAt = tournament.CreatedAt,
                TournamentId = tournament.TournamentId,
                TournamentName = tournament.Tournament?.FullName,
                TournamentCodes = tournament.TournamentCodes.Select(c => new RiotTournamentCodeItemViewModel
                {
                    Id = c.Id,
                    Code = c.Code,
                    Description = c.Description,
                    SeriesId = c.SeriesId,
                    SeriesName = c.Series != null ? $"{c.Series.TeamA?.Tag} vs {c.Series.TeamB?.Tag}" : null,
                    TeamAId = c.TeamAId,
                    TeamAName = c.TeamA?.Tag,
                    TeamBId = c.TeamBId,
                    TeamBName = c.TeamB?.Tag,
                    MapType = c.MapType,
                    PickType = c.PickType,
                    SpectatorType = c.SpectatorType,
                    TeamSize = c.TeamSize,
                    CreatedAt = c.CreatedAt,
                    IsUsed = c.IsUsed,
                    MatchId = c.MatchId,
                    MatchDbId = c.MatchDbId
                }).OrderByDescending(c => c.CreatedAt).ToList()
            };

            return View("RiotTournaments/Details", vm);
        }

        /// <summary>
        /// Show form to generate tournament codes
        /// </summary>
        [HttpGet]
        [Route("admin/riot-tournaments/{id}/generate-codes")]
        public async Task<IActionResult> GenerateTournamentCodes(int id, [FromServices] IRiotTournamentService tournamentService)
        {
            var tournament = await tournamentService.GetTournamentByIdAsync(id);

            if (tournament == null)
                return NotFound();

            // Get available series from the linked tournament
            var series = new List<SelectListItem>();
            if (tournament.TournamentId.HasValue)
            {
                series = await _context.Series
                    .Where(s => s.TournamentId == tournament.TournamentId.Value)
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = $"{s.TeamA.Tag} vs {s.TeamB.Tag} - {s.Round}"
                    })
                    .ToListAsync();
            }

            var teams = await _context.Teams
                .Where(t => t.Game.FullName == "League of Legends")
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = $"{t.Tag} - {t.FullName}"
                })
                .ToListAsync();

            var vm = new GenerateTournamentCodesViewModel
            {
                RiotTournamentId = tournament.Id,
                TournamentName = tournament.Name,
                AvailableSeries = series,
                AvailableTeams = teams
            };

            return View("RiotTournaments/GenerateCodes", vm);
        }

        /// <summary>
        /// Generate tournament codes
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/riot-tournaments/{id}/generate-codes")]
        public async Task<IActionResult> GenerateTournamentCodes(int id, GenerateTournamentCodesViewModel model, [FromServices] IRiotTournamentService tournamentService)
        {
            if (!ModelState.IsValid)
            {
                var tournament = await tournamentService.GetTournamentByIdAsync(id);
                model.TournamentName = tournament?.Name;

                // Reload dropdowns
                var series = new List<SelectListItem>();
                if (tournament?.TournamentId.HasValue == true)
                {
                    series = await _context.Series
                        .Where(s => s.TournamentId == tournament.TournamentId.Value)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Id.ToString(),
                            Text = $"{s.TeamA.Tag} vs {s.TeamB.Tag} - {s.Round}"
                        })
                        .ToListAsync();
                }

                var teams = await _context.Teams
                    .Where(t => t.Game.FullName == "League of Legends")
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = $"{t.Tag} - {t.FullName}"
                    })
                    .ToListAsync();

                model.AvailableSeries = series;
                model.AvailableTeams = teams;

                return View("RiotTournaments/GenerateCodes", model);
            }

            try
            {
                var tournament = await tournamentService.GetTournamentByIdAsync(id);
                
                var codes = await tournamentService.GenerateTournamentCodesAsync(
                    tournament.RiotTournamentId,
                    model.Count,
                    model.SeriesId,
                    model.TeamAId,
                    model.TeamBId,
                    model.Description,
                    model.MapType,
                    model.PickType,
                    model.SpectatorType,
                    model.TeamSize,
                    null,
                    model.Metadata);

                TempData["SuccessMessage"] = $"Successfully generated {codes.Count} tournament code(s)!";
                return RedirectToAction("RiotTournamentDetails", new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error generating codes: {ex.Message}");

                var tournament = await tournamentService.GetTournamentByIdAsync(id);
                model.TournamentName = tournament?.Name;

                // Reload dropdowns
                var series = new List<SelectListItem>();
                if (tournament?.TournamentId.HasValue == true)
                {
                    series = await _context.Series
                        .Where(s => s.TournamentId == tournament.TournamentId.Value)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Id.ToString(),
                            Text = $"{s.TeamA.Tag} vs {s.TeamB.Tag} - {s.Round}"
                        })
                        .ToListAsync();
                }

                var teams = await _context.Teams
                    .Where(t => t.Game.FullName == "League of Legends")
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = $"{t.Tag} - {t.FullName}"
                    })
                    .ToListAsync();

                model.AvailableSeries = series;
                model.AvailableTeams = teams;

                return View("RiotTournaments/GenerateCodes", model);
            }
        }

        /// <summary>
        /// Check for matches played with a tournament code
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/riot-tournaments/check-code")]
        public async Task<IActionResult> CheckTournamentCode([FromBody] CheckCodeRequest request, [FromServices] IRiotTournamentService tournamentService)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
                return Json(new { success = false, message = "Code is required" });

            var code = request.Code.Trim();
            try
            {
                var matchIds = await tournamentService.GetMatchIdsByTournamentCodeAsync(code);

                if (matchIds != null && matchIds.Any())
                {
                    // Get the tournament code to find the platform region
                    var tournamentCode = await _context.Set<RiotTournamentCode>()
                        .Include(tc => tc.RiotTournament)
                        .FirstOrDefaultAsync(tc => tc.Code == code);

                    if (tournamentCode == null)
                        return Json(new { success = false, message = "Tournament code not found in database" });

                    // Format match ID as PLATFORM_GAMEID (e.g., EUW1_1234567890)
                    var platform = tournamentCode.RiotTournament.Region; // e.g., "EUW1"
                    var matchId = $"{platform}_{matchIds[0]}";
                    
                    await tournamentService.UpdateTournamentCodeWithMatchAsync(code, matchId);

                    return Json(new { success = true, matchId, matchIds, message = $"Found {matchIds.Count} match(es). Match ID: {matchId}" });
                }

                return Json(new { success = false, message = "No matches found for this code yet" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Display Riot match details (champions, items, stats) - fetches from Match-v5 API
        /// </summary>
        [HttpGet]
        [Route("admin/riot-tournaments/match/{matchId}")]
        public async Task<IActionResult> RiotMatchDetails(
            string matchId,
            [FromServices] Balkana.Services.Riot.IRiotMatchApiService matchApi,
            [FromServices] Balkana.Services.Riot.IDDragonVersionService ddragonVersion,
            int? pendingId = null)
        {
            if (string.IsNullOrWhiteSpace(matchId))
                return NotFound();

            var match = await matchApi.GetMatchAsync(matchId);
            if (match == null)
                return NotFound("Match not found or inaccessible via Riot API");

            var ddragonVer = await ddragonVersion.GetDDragonVersionAsync(match.info?.gameVersion);
            var blueTeam = match.info?.participants?.Where(p => p.teamId == 100).ToList() ?? new List<Balkana.Data.DTOs.Riot.RiotParticipantDto>();
            var redTeam = match.info?.participants?.Where(p => p.teamId == 200).ToList() ?? new List<Balkana.Data.DTOs.Riot.RiotParticipantDto>();
            var blueWon = match.info?.teams?.FirstOrDefault(t => t.teamId == 100)?.win ?? false;

            ViewBag.PendingId = pendingId;

            var vm = new RiotMatchDetailsViewModel
            {
                MatchId = match.metadata?.matchId ?? matchId,
                GameVersion = match.info?.gameVersion ?? "",
                DDragonVersion = ddragonVer,
                GameDurationSeconds = match.info?.gameDuration ?? 0,
                GameMode = match.info?.gameMode ?? "",
                GameType = match.info?.gameType ?? "",
                GameStartTimestamp = match.info?.gameStartTimestamp ?? 0,
                MapId = match.info?.mapId ?? 0,
                BlueTeamWon = blueWon,
                BlueTeam = blueTeam.Select(MapParticipant).ToList(),
                RedTeam = redTeam.Select(MapParticipant).ToList()
            };

            return View("RiotTournaments/RiotMatchDetails", vm);
        }

        /// <summary>
        /// List pending Riot match callbacks for manual import.
        /// </summary>
        [HttpGet]
        [Route("admin/riot-tournaments/pending-matches")]
        public async Task<IActionResult> RiotPendingMatches(string? status = null)
        {
            var query = _context.RiotPendingMatches
                .Include(p => p.RiotTournamentCode)
                .AsQueryable();

            if (string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
                query = query.Where(p => p.Status == RiotPendingMatchStatus.Pending);
            else if (string.Equals(status, "imported", StringComparison.OrdinalIgnoreCase))
                query = query.Where(p => p.Status == RiotPendingMatchStatus.Imported);

            var list = await query.OrderByDescending(p => p.CreatedAt).Take(200).ToListAsync();

            var vm = new RiotPendingMatchesViewModel
            {
                Items = list.Select(p => new RiotPendingMatchItemViewModel
                {
                    Id = p.Id,
                    MatchId = p.MatchId,
                    TournamentCode = p.TournamentCode,
                    LinkedCode = p.RiotTournamentCode?.Code,
                    LinkedSeriesId = p.RiotTournamentCode?.SeriesId,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    ErrorMessage = p.ErrorMessage
                }).ToList()
            };

            return View("RiotTournaments/PendingMatches", vm);
        }

        /// <summary>
        /// Import form for a pending match.
        /// </summary>
        [HttpGet]
        [Route("admin/riot-tournaments/pending-matches/{id}/import")]
        public async Task<IActionResult> ImportPendingMatch(int id)
        {
            var pending = await _context.RiotPendingMatches
                .Include(p => p.RiotTournamentCode)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pending == null)
                return NotFound();

            if (pending.Status != RiotPendingMatchStatus.Pending)
                return BadRequest($"Match is already {pending.Status}");

            var series = await _context.Series
                .Include(s => s.Tournament)
                .Where(s => s.Tournament.Game.ShortName == "LoL")
                .OrderByDescending(s => s.DatePlayed)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Tournament.FullName} - {s.Name} ({s.DatePlayed:yyyy-MM-dd})"
                })
                .ToListAsync();

            var vm = new ImportPendingMatchViewModel
            {
                PendingMatchId = id,
                MatchId = pending.MatchId,
                TournamentCode = pending.TournamentCode,
                SuggestedSeriesId = pending.RiotTournamentCode?.SeriesId,
                Series = series
            };

            return View("RiotTournaments/ImportPendingMatch", vm);
        }

        /// <summary>
        /// Import a pending match into the selected series.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/riot-tournaments/pending-matches/{id}/import")]
        public async Task<IActionResult> ImportPendingMatch(int id, ImportPendingMatchViewModel model,
            [FromServices] IRiotPendingMatchImportService importService)
        {
            if (id != model.PendingMatchId)
                return BadRequest();

            var (success, error) = await importService.ImportAsync(id, model.SelectedSeriesId);

            if (success)
            {
                TempData["SuccessMessage"] = $"Match {model.MatchId} imported successfully.";
                return RedirectToAction("Details", "Series", new { id = model.SelectedSeriesId });
            }

            ModelState.AddModelError("", error ?? "Import failed.");
            var series = await _context.Series
                .Include(s => s.Tournament)
                .Where(s => s.Tournament.Game.ShortName == "LoL")
                .OrderByDescending(s => s.DatePlayed)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = $"{s.Tournament.FullName} - {s.Name} ({s.DatePlayed:yyyy-MM-dd})" })
                .ToListAsync();
            model.Series = series;
            return View("RiotTournaments/ImportPendingMatch", model);
        }

        /// <summary>
        /// Discard a pending match (mark as Discarded).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/riot-tournaments/pending-matches/{id}/discard")]
        public async Task<IActionResult> DiscardPendingMatch(int id)
        {
            var pending = await _context.RiotPendingMatches.FindAsync(id);
            if (pending == null)
                return NotFound();

            pending.Status = RiotPendingMatchStatus.Discarded;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pending match discarded.";
            return RedirectToAction("RiotPendingMatches");
        }

        private static RiotMatchParticipantViewModel MapParticipant(Balkana.Data.DTOs.Riot.RiotParticipantDto p)
        {
            var riotId = "";
            if (!string.IsNullOrEmpty(p.riotIdGameName) && !string.IsNullOrEmpty(p.riotIdTagline))
                riotId = $"{p.riotIdGameName}#{p.riotIdTagline}";

            return new RiotMatchParticipantViewModel
            {
                Puuid = p.puuid ?? "",
                RiotId = riotId,
                ChampionName = p.championName ?? "",
                ChampionId = p.championId,
                TeamPosition = p.teamPosition ?? "",
                ChampLevel = p.champLevel,
                Kills = p.kills,
                Deaths = p.deaths,
                Assists = p.assists,
                GoldEarned = p.goldEarned,
                CreepScore = p.totalMinionsKilled + p.neutralMinionsKilled,
                VisionScore = p.visionScore,
                TotalDamageToChampions = p.totalDamageDealtToChampions,
                DamageToObjectives = p.damageDealtToObjectives,
                Item0 = p.item0, Item1 = p.item1, Item2 = p.item2, Item3 = p.item3,
                Item4 = p.item4, Item5 = p.item5, Item6 = p.item6,
                Summoner1Id = p.summoner1Id,
                Summoner2Id = p.summoner2Id
            };
        }

        // ============================================
        // STORE MANAGEMENT
        // ============================================

        [HttpGet]
        [Route("admin/store/products")]
        public async Task<IActionResult> StoreProducts([FromServices] IAdminStoreService adminStore)
        {
            var products = await adminStore.GetAllProductsAsync();
            return View("Store/Products", products);
        }

        [HttpGet]
        [Route("admin/store/products/create")]
        public async Task<IActionResult> CreateProduct()
        {
            var categories = await _context.ProductCategories
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();

            var teams = await _context.Teams
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = $"{t.Tag} - {t.FullName}" })
                .ToListAsync();

            var players = await _context.Players
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Nickname })
                .ToListAsync();

            var model = new AdminProductFormViewModel
            {
                Categories = categories,
                Teams = teams,
                Players = players
            };

            return View("Store/ProductForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/store/products/create")]
        public async Task<IActionResult> CreateProduct(AdminProductFormViewModel model, [FromServices] IAdminStoreService adminStore)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.ProductCategories
                    .Where(c => c.IsActive)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync();

                model.Teams = await _context.Teams
                    .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = $"{t.Tag} - {t.FullName}" })
                    .ToListAsync();

                model.Players = await _context.Players
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Nickname })
                    .ToListAsync();

                return View("Store/ProductForm", model);
            }

            try
            {
                var productId = await adminStore.CreateProductAsync(model);
                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction("ProductVariants", new { id = productId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Store/ProductForm", model);
            }
        }

        [HttpGet]
        [Route("admin/store/products/{id}/variants")]
        public async Task<IActionResult> ProductVariants(int id, [FromServices] IAdminStoreService adminStore)
        {
            var product = await adminStore.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            ViewBag.Product = product;
            var variants = await adminStore.GetProductVariantsAsync(id);
            
            return View("Store/ProductVariants", variants);
        }

        [HttpGet]
        [Route("admin/store/products/{productId}/variants/create")]
        public async Task<IActionResult> CreateProductVariant(int productId, [FromServices] IAdminStoreService adminStore)
        {
            var product = await adminStore.GetProductByIdAsync(productId);
            if (product == null)
                return NotFound();

            var model = new AdminProductVariantFormViewModel
            {
                ProductId = productId,
                ProductName = product.Name,
                Price = product.BasePrice
            };

            return View("Store/ProductVariantForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/store/products/{productId}/variants/create")]
        public async Task<IActionResult> CreateProductVariant(int productId, AdminProductVariantFormViewModel model, [FromServices] IAdminStoreService adminStore)
        {
            if (!ModelState.IsValid)
            {
                var product = await adminStore.GetProductByIdAsync(productId);
                model.ProductName = product?.Name;
                return View("Store/ProductVariantForm", model);
            }

            try
            {
                var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                await adminStore.CreateProductVariantAsync(model, userId);
                TempData["SuccessMessage"] = "Variant created successfully!";
                return RedirectToAction("ProductVariants", new { id = productId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Store/ProductVariantForm", model);
            }
        }

        [HttpGet]
        [Route("admin/store/orders")]
        public async Task<IActionResult> StoreOrders([FromServices] IAdminStoreService adminStore, OrderStatus? status, int page = 1)
        {
            var orders = await adminStore.GetAllOrdersAsync(status, page);
            ViewBag.StatusFilter = status;
            return View("Store/Orders", orders);
        }

        [HttpGet]
        [Route("admin/store/orders/{id}")]
        public async Task<IActionResult> StoreOrderDetails(int id, [FromServices] IAdminStoreService adminStore)
        {
            var order = await adminStore.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            return View("Store/OrderDetails", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/store/orders/{id}/update-status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus newStatus, string notes, [FromServices] IAdminStoreService adminStore)
        {
            try
            {
                await adminStore.UpdateOrderStatusAsync(id, newStatus, notes);
                TempData["SuccessMessage"] = "Order status updated!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("StoreOrderDetails", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/store/orders/{id}/add-tracking")]
        public async Task<IActionResult> AddTracking(int id, string trackingNumber, [FromServices] IAdminStoreService adminStore)
        {
            try
            {
                await adminStore.AddTrackingNumberAsync(id, trackingNumber);
                await adminStore.UpdateOrderStatusAsync(id, OrderStatus.Shipped, $"Tracking number: {trackingNumber}");
                TempData["SuccessMessage"] = "Tracking number added and order marked as shipped!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("StoreOrderDetails", new { id });
        }

        [HttpGet]
        [Route("admin/store/inventory")]
        public async Task<IActionResult> Inventory([FromServices] IAdminStoreService adminStore)
        {
            var lowStock = await adminStore.GetLowStockProductsAsync();
            return View("Store/Inventory", lowStock);
        }

        [HttpGet]
        [Route("admin/store/collections")]
        public async Task<IActionResult> StoreCollections([FromServices] IAdminStoreService adminStore)
        {
            var collections = await adminStore.GetAllCollectionsAsync();
            return View("Store/Collections", collections);
        }

        [HttpGet]
        [Route("admin/store/collections/create")]
        public IActionResult CreateCollection()
        {
            return View("Store/CollectionForm", new AdminCollectionFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/store/collections/create")]
        public async Task<IActionResult> CreateCollection(AdminCollectionFormViewModel model, [FromServices] IAdminStoreService adminStore)
        {
            if (!ModelState.IsValid)
            {
                return View("Store/CollectionForm", model);
            }

            try
            {
                var id = await adminStore.CreateCollectionAsync(model);
                TempData["SuccessMessage"] = "Collection created successfully!";
                return RedirectToAction("StoreCollections");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Store/CollectionForm", model);
            }
        }
    }
}
