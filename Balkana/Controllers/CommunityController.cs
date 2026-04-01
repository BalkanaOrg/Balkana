using Balkana.Data.Models;
using Balkana.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Balkana.Models.Community;
using Microsoft.AspNetCore.Mvc.Rendering;
using Balkana.Services.Community;

namespace Balkana.Controllers
{
    public class CommunityController : Controller
    {
        private readonly ApplicationDbContext data;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CommunityController> _logger;
        private readonly CommunityApprovalService _approval;

        public CommunityController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<CommunityController> logger, CommunityApprovalService approval)
        {
            data = db;
            _userManager = userManager;
            _logger = logger;
            _approval = approval;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var currentUserId = _userManager.GetUserId(User);

            // usersQuery is an IQueryable<ApplicationUser>
            IQueryable<ApplicationUser> usersQuery = _userManager.Users;

            // teamsQuery as IQueryable<CommunityTeam> so Where/Include assignments stay compatible
            IQueryable<CommunityTeam> teamsQuery = data.CommunityTeams
                .Include(t => t.Members)        // Include is fine, returns a subtype of IQueryable
                    .ThenInclude(m => m.User);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                usersQuery = usersQuery.Where(u =>
                    u.UserName.Contains(searchTerm) ||
                    (u.FirstName != null && u.FirstName.Contains(searchTerm)) ||
                    (u.LastName != null && u.LastName.Contains(searchTerm)));

                teamsQuery = teamsQuery.Where(t =>
                    t.FullName.Contains(searchTerm) ||
                    t.Tag.Contains(searchTerm));
            }

            var users = await usersQuery
                .OrderBy(u => u.UserName)
                .Take(20)
                .Select(u => new CommunityUserSearchModel
                {
                    UserId = u.Id,
                    Username = u.UserName,
                    FullName = (u.FirstName ?? "") + " " + (u.LastName ?? "")
                })
                .ToListAsync();

            var teams = await teamsQuery
                .OrderBy(t => t.FullName)
                .Take(20)
                .Select(t => new CommunityTeamSearchModel
                {
                    Id = t.Id,
                    Tag = t.Tag,
                    FullName = t.FullName,
                    LogoUrl = t.LogoUrl,
                    IsApproved = t.IsApproved,

                    PlayersCount = t.Members.Count(m => m.Role != CommunityMemberRole.Substitute && m.Role != CommunityMemberRole.Coach),
                    SubstitutesCount = t.Members.Count(m => m.Role == CommunityMemberRole.Substitute),
                    CoachesCount = t.Members.Count(m => m.Role == CommunityMemberRole.Coach),

                    IsMember = currentUserId != null && t.Members.Any(m => m.UserId == currentUserId)
                })
                .ToListAsync();

            var model = new CommunityIndexViewModel
            {
                SearchTerm = searchTerm,
                Users = users,
                Teams = teams,
                CanModerate = User.IsInRole("Administrator") || User.IsInRole("Moderator")
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RequestJoin(int teamId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var alreadyMember = await data.CommunityTeamMembers
                .AnyAsync(m => m.CommunityTeamId == teamId && m.UserId == user.Id);
            if (alreadyMember) return BadRequest("You are already a member of this team.");

            var alreadyRequested = await data.CommunityJoinRequests
                .AnyAsync(r => r.CommunityTeamId == teamId && r.UserId == user.Id);
            if (alreadyRequested) return BadRequest("You already have a pending join request for this team.");

            var request = new CommunityJoinRequest
            {
                CommunityTeamId = teamId,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            data.CommunityJoinRequests.Add(request);
            await data.SaveChangesAsync();

            TempData["Success"] = "Join request sent to team captain.";
            return RedirectToAction("Index");
        }

        // Create a community team
        [HttpGet]
        [Authorize]
        public IActionResult CreateTeam()
        {
            var games = data.Games
                .Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = g.FullName
                })
                .ToList();

            var model = new CreateCommunityTeamModel
            {
                Games = games
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTeam(CreateCommunityTeamModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Games = data.Games
                    .Select(g => new SelectListItem
                    {
                        Value = g.Id.ToString(),
                        Text = g.FullName
                    })
                    .ToList();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var team = new CommunityTeam
            {
                Tag = model.Tag,
                FullName = model.FullName,
                LogoUrl = model.LogoUrl,
                GameId = model.GameId,
                IsApproved = false
            };

            data.CommunityTeams.Add(team);
            await data.SaveChangesAsync();

            var captain = new CommunityTeamMember
            {
                CommunityTeamId = team.Id,
                UserId = user.Id,
                Role = CommunityMemberRole.Captain,
                IsApproved = false
            };

            data.CommunityTeamMembers.Add(captain);
            await data.SaveChangesAsync();

            TempData["Success"] = "Community team created — awaiting moderator approval.";
            return RedirectToAction("TeamDetails", new { id = team.Id });
        }

        [HttpGet]
        public async Task<IActionResult> TeamDetails(int id)
        {
            var team = await data.CommunityTeams
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .Include(t => t.Game)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null) return NotFound();

            var captain = team.Members.FirstOrDefault(m => m.Role == CommunityMemberRole.Captain);
            var currentUserId = _userManager.GetUserId(User);

            var requiredAccountType = _approval.GetRequiredLinkedAccountType(team.Game);
            Dictionary<string, UserLinkedAccount> requiredLinksByUserId = new();
            if (requiredAccountType != null)
            {
                var memberIds = team.Members.Select(m => m.UserId).ToList();
                requiredLinksByUserId = await data.UserLinkedAccounts
                    .Where(ula => memberIds.Contains(ula.UserId) && ula.Type == requiredAccountType)
                    .ToDictionaryAsync(ula => ula.UserId);
            }

            var model = new CommunityTeamDetailsModel
            {
                Id = team.Id,
                Tag = team.Tag,
                FullName = team.FullName,
                LogoUrl = team.LogoUrl,
                IsApproved = team.IsApproved,
                RequiredAccountType = requiredAccountType,
                CaptainName = captain?.User?.UserName ?? "Unknown",
                CaptainId = captain?.UserId ?? "",
                CanManageTeam = currentUserId != null && captain != null && captain.UserId == currentUserId,
                CanModerate = User.IsInRole("Administrator") || User.IsInRole("Moderator"),
                Members = team.Members.Select(m => new CommunityTeamMemberModel
                {
                    UserId = m.UserId,
                    Username = m.User.UserName,
                    Role = m.Role.ToString(),
                    IsApproved = m.IsApproved,
                    HasPlayerLinked = m.User.PlayerId != null,
                    HasRequiredLinkedAccount = requiredAccountType == null || requiredLinksByUserId.ContainsKey(m.UserId),
                    RequiredAccountType = requiredAccountType,
                    RequiredAccountDisplayName = requiredAccountType != null && requiredLinksByUserId.TryGetValue(m.UserId, out var link)
                        ? (link.DisplayName ?? link.Identifier)
                        : null
                }).ToList()
            };

            return View(model);
        }

        // Captain invites a member
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Invite(int teamId, string inviteeUserId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            // check captain role
            var isCaptain = await data.CommunityTeamMembers
                .AnyAsync(m => m.CommunityTeamId == teamId && m.UserId == currentUser.Id && m.Role == CommunityMemberRole.Captain);

            if (!isCaptain) return Forbid();

            // ensure invitee exists
            var invitee = await _userManager.FindByIdAsync(inviteeUserId);
            if (invitee == null) return NotFound();

            // Prevent duplicates
            var alreadyMember = await data.CommunityTeamMembers.AnyAsync(m => m.CommunityTeamId == teamId && m.UserId == inviteeUserId);
            if (alreadyMember) return BadRequest("User already member");

            var existingPendingInvite = await data.CommunityInvites.AnyAsync(i =>
                i.CommunityTeamId == teamId &&
                i.InviteeUserId == inviteeUserId &&
                !i.IsAccepted &&
                !i.IsCancelled &&
                (!i.ExpiresAt.HasValue || i.ExpiresAt >= DateTime.UtcNow));
            if (existingPendingInvite) return BadRequest("User already has a pending invite for this team.");

            var invite = new CommunityInvite
            {
                CommunityTeamId = teamId,
                InviterUserId = currentUser.Id,
                InviteeUserId = inviteeUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            data.CommunityInvites.Add(invite);
            await data.SaveChangesAsync();

            // TODO: notify invitee (email/in-app)
            return Ok(new { success = true, inviteId = invite.Id });
        }

        // Invitee accepts invite
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AcceptInvite(int inviteId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var invite = await data.CommunityInvites.Include(i => i.CommunityTeam).FirstOrDefaultAsync(i => i.Id == inviteId);

            if (invite == null || invite.InviteeUserId != user.Id) return NotFound();
            if (invite.IsAccepted || invite.IsCancelled) return BadRequest();
            if (invite.ExpiresAt.HasValue && invite.ExpiresAt < DateTime.UtcNow) return BadRequest("Invite expired");

            invite.IsAccepted = true;
            invite.AcceptedAt = DateTime.UtcNow;

            var alreadyMember = await data.CommunityTeamMembers.AnyAsync(m =>
                m.CommunityTeamId == invite.CommunityTeamId && m.UserId == user.Id);

            // add member (unapproved by moderator yet)
            if (!alreadyMember)
            {
                var member = new CommunityTeamMember
                {
                    CommunityTeamId = invite.CommunityTeamId,
                    UserId = user.Id,
                    Role = CommunityMemberRole.Player,
                    IsApproved = false
                };
                data.CommunityTeamMembers.Add(member);
            }
            await data.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TeamRequests(int teamId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var isCaptain = await data.CommunityTeamMembers
                .AnyAsync(m => m.CommunityTeamId == teamId && m.UserId == currentUser.Id && m.Role == CommunityMemberRole.Captain);

            if (!isCaptain) return Forbid(); // <- you’ll stay on the same page, no redirect

            var requests = await data.CommunityJoinRequests
                .Include(r => r.User)
                .Where(r => r.CommunityTeamId == teamId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var model = requests.Select(r => new
            {
                r.Id,
                Username = r.User.UserName,
                r.CreatedAt
            });

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ApproveJoinRequest(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var req = await data.CommunityJoinRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null) return NotFound();

            var isCaptain = await data.CommunityTeamMembers.AnyAsync(m =>
                m.CommunityTeamId == req.CommunityTeamId &&
                m.UserId == currentUser.Id &&
                m.Role == CommunityMemberRole.Captain);
            if (!isCaptain) return Forbid();

            var alreadyMember = await data.CommunityTeamMembers.AnyAsync(m => m.CommunityTeamId == req.CommunityTeamId && m.UserId == req.UserId);
            if (!alreadyMember)
            {
            // Add as member (unapproved by moderator yet)
            var member = new CommunityTeamMember
            {
                CommunityTeamId = req.CommunityTeamId,
                UserId = req.UserId,
                Role = CommunityMemberRole.Player,
                IsApproved = false
            };
            data.CommunityTeamMembers.Add(member);
            }

            data.CommunityJoinRequests.Remove(req);
            await data.SaveChangesAsync();

            return RedirectToAction("TeamRequests", new { teamId = req.CommunityTeamId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RejectJoinRequest(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var req = await data.CommunityJoinRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null) return NotFound();

            var isCaptain = await data.CommunityTeamMembers.AnyAsync(m =>
                m.CommunityTeamId == req.CommunityTeamId &&
                m.UserId == currentUser.Id &&
                m.Role == CommunityMemberRole.Captain);
            if (!isCaptain) return Forbid();

            data.CommunityJoinRequests.Remove(req);
            await data.SaveChangesAsync();

            return RedirectToAction("TeamRequests", new { teamId = req.CommunityTeamId });
        }

        // Moderator approves a team
        [Authorize(Roles = "Administrator,Moderator")]
        [HttpPost]
        public async Task<IActionResult> ApproveTeam(int teamId)
        {
            var team = await data.CommunityTeams
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .Include(t => t.Game)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return NotFound();

            team.IsApproved = true;
            team.ApprovedAt = DateTime.UtcNow;
            team.ApprovedBy = User.Identity.Name;

            await data.SaveChangesAsync();

            // If members are already approved, ensure Player records are linked.
            foreach (var m in team.Members.Where(m => m.IsApproved))
            {
                await _approval.EnsurePlayerLinkedAsync(m.UserId);
            }

            if (Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { success = true });
            }

            return RedirectToAction(nameof(InspectTeam), new { teamId });
        }

        // Moderator approves a member (promote their community user to official Player)
        [Authorize(Roles = "Administrator,Moderator")]
        [HttpPost]
        public async Task<IActionResult> ApproveMember(int teamId, string userId)
        {
            var member = await data.CommunityTeamMembers
                .Include(m => m.User)
                .Include(m => m.CommunityTeam)
                    .ThenInclude(t => t.Game)
                .FirstOrDefaultAsync(m => m.CommunityTeamId == teamId && m.UserId == userId);
            if (member == null) return NotFound();

            var requiredAccountType = _approval.GetRequiredLinkedAccountType(member.CommunityTeam.Game);
            if (requiredAccountType != null)
            {
                var linked = await _approval.GetUserLinkedAccountAsync(userId, requiredAccountType);
                if (linked == null)
                {
                    if (Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest($"User is missing required linked account: {requiredAccountType}");
                    }

                    TempData["Error"] = $"Cannot approve member: missing required {requiredAccountType} linked account.";
                    return RedirectToAction(nameof(InspectTeam), new { teamId });
                }
            }

            member.IsApproved = true;
            member.ApprovedAt = DateTime.UtcNow;
            member.ApprovedBy = User.Identity.Name;

            await data.SaveChangesAsync();

            await _approval.EnsurePlayerLinkedAsync(userId);

            if (Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { success = true });
            }

            return RedirectToAction(nameof(InspectTeam), new { teamId });
        }

        [Authorize(Roles = "Administrator,Moderator")]
        [HttpGet]
        public async Task<IActionResult> Moderation()
        {
            var teams = await data.CommunityTeams
                .Include(t => t.Game)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var teamIds = teams.Select(t => t.Id).ToList();
            var allUserIds = teams.SelectMany(t => t.Members.Select(m => m.UserId)).Distinct().ToList();

            var linked = await data.UserLinkedAccounts
                .Where(ula => allUserIds.Contains(ula.UserId) && (ula.Type == "FaceIt" || ula.Type == "Riot"))
                .ToListAsync();

            var linkedLookup = linked
                .GroupBy(l => (l.UserId, l.Type))
                .ToDictionary(g => g.Key, g => g.First());

            var vm = new CommunityModerationViewModel();
            foreach (var t in teams)
            {
                var required = _approval.GetRequiredLinkedAccountType(t.Game);
                var members = t.Members.ToList();

                var missingRequired = 0;
                if (required != null)
                {
                    missingRequired = members.Count(m => !linkedLookup.ContainsKey((m.UserId, required)));
                }

                vm.Teams.Add(new CommunityModerationTeamItemViewModel
                {
                    TeamId = t.Id,
                    Tag = t.Tag,
                    FullName = t.FullName,
                    LogoUrl = t.LogoUrl,
                    GameName = t.Game?.FullName ?? "",
                    TeamApproved = t.IsApproved,
                    TotalMembers = members.Count,
                    ApprovedMembers = members.Count(m => m.IsApproved),
                    UnapprovedMembers = members.Count(m => !m.IsApproved),
                    RequiredAccountType = required,
                    MissingRequiredAccounts = missingRequired,
                    MissingPlayerLinks = members.Count(m => m.User.PlayerId == null)
                });
            }

            // Focus the queue on actionable items
            vm.Teams = vm.Teams
                .Where(t => !t.TeamApproved || t.UnapprovedMembers > 0 || t.MissingRequiredAccounts > 0 || t.MissingPlayerLinks > 0)
                .ToList();

            return View(vm);
        }

        [Authorize(Roles = "Administrator,Moderator")]
        [HttpGet]
        public async Task<IActionResult> InspectTeam(int teamId)
        {
            var team = await data.CommunityTeams
                .Include(t => t.Game)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return NotFound();

            var required = _approval.GetRequiredLinkedAccountType(team.Game);
            var memberIds = team.Members.Select(m => m.UserId).ToList();

            Dictionary<string, UserLinkedAccount> requiredLinksByUserId = new();
            if (required != null)
            {
                requiredLinksByUserId = await data.UserLinkedAccounts
                    .Where(ula => memberIds.Contains(ula.UserId) && ula.Type == required)
                    .ToDictionaryAsync(ula => ula.UserId);
            }

            var vm = new CommunityTeamInspectionViewModel
            {
                TeamId = team.Id,
                Tag = team.Tag,
                FullName = team.FullName,
                LogoUrl = team.LogoUrl,
                GameName = team.Game?.FullName ?? "",
                TeamApproved = team.IsApproved,
                RequiredAccountType = required,
                Members = team.Members
                    .OrderByDescending(m => m.Role == CommunityMemberRole.Captain)
                    .ThenBy(m => m.User.UserName)
                    .Select(m => new CommunityTeamInspectionMemberViewModel
                    {
                        UserId = m.UserId,
                        Username = m.User.UserName,
                        Role = m.Role.ToString(),
                        MemberApproved = m.IsApproved,
                        HasPlayerLinked = m.User.PlayerId != null,
                        HasRequiredLinkedAccount = required == null || requiredLinksByUserId.ContainsKey(m.UserId),
                        RequiredAccountDisplayName = required != null && requiredLinksByUserId.TryGetValue(m.UserId, out var link)
                            ? (link.DisplayName ?? link.Identifier)
                            : null
                    })
                    .ToList()
            };

            return View(vm);
        }

        // Admin action: finalize (create official Team, Players, and PlayerTeamTransfer rows)
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> PromoteCommunityTeam(int teamId)
        {
            // Load community team with approved members
            var ct = await data.CommunityTeams
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .Include(t => t.Transfers)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (ct == null) return NotFound();
            if (!ct.IsApproved) return BadRequest("Community team is not approved");

            // Ensure minimum approved players (>=3)
            var approvedMembers = ct.Members.Where(m => m.IsApproved).ToList();
            if (approvedMembers.Count < 3) return BadRequest("Need at least 3 approved players to promote");

            // Create official Team (branding)
            var officialTeam = new Team
            {
                Tag = ct.Tag,
                FullName = ct.FullName,
                LogoURL = ct.LogoUrl ?? "",
                GameId = ct.GameId,
                yearFounded = DateTime.UtcNow.Year
            };

            data.Teams.Add(officialTeam);
            await data.SaveChangesAsync();

            // For each approved community member: create Player (if not exists) and PlayerTeamTransfer
            foreach (var member in approvedMembers)
            {
                var linkedPlayerId = member.User.PlayerId ?? await _approval.EnsurePlayerLinkedAsync(member.UserId);
                if (linkedPlayerId == null) return BadRequest("Unable to link Player for a team member.");

                // create official transfer into the new team at current datetime (or use the community transfer date)
                var pw = new PlayerTeamTransfer
                {
                    PlayerId = linkedPlayerId.Value,
                    TeamId = officialTeam.Id,
                    StartDate = DateTime.UtcNow,
                    PositionId = ct.Transfers.FirstOrDefault(t => t.UserId == member.UserId)?.PositionId ?? 0,
                    EndDate = null,
                    Status = PlayerTeamStatus.Active
                };

                data.PlayerTeamTransfers.Add(pw);
            }

            await data.SaveChangesAsync();

            // Optional: mark community team as promoted (to prevent double-promote)
            // maybe add PromotedToTeamId property to CommunityTeam; left as exercise

            return Ok(new { success = true, teamId = officialTeam.Id });
        }
    }
}
