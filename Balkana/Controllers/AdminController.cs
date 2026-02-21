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

        public AdminController(
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
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
