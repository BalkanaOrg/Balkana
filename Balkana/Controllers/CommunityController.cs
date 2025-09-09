using Balkana.Data.Models;
using Balkana.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Balkana.Models.Community;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Controllers
{
    public class CommunityController : Controller
    {
        private readonly ApplicationDbContext data;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CommunityController> _logger;

        public CommunityController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<CommunityController> logger)
        {
            data = db;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm)
        {
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
                    LogoUrl = t.LogoUrl
                })
                .ToListAsync();

            var model = new CommunityIndexViewModel
            {
                SearchTerm = searchTerm,
                Users = users,
                Teams = teams
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RequestJoin(int teamId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var alreadyMember = await data.CommunityTeamMembers
                .AnyAsync(m => m.CommunityTeamId == teamId && m.UserId == user.Id);
            if (alreadyMember) return BadRequest("You are already a member of this team.");

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
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null) return NotFound();

            var captain = team.Members.FirstOrDefault(m => m.Role == CommunityMemberRole.Captain);

            var model = new CommunityTeamDetailsModel
            {
                Id = team.Id,
                Tag = team.Tag,
                FullName = team.FullName,
                LogoUrl = team.LogoUrl,
                IsApproved = team.IsApproved,
                CaptainName = captain?.User?.UserName ?? "Unknown",
                Members = team.Members.Select(m => new CommunityTeamMemberModel
                {
                    Username = m.User.UserName,
                    Role = m.Role.ToString(),
                    IsApproved = m.IsApproved
                }).ToList()
            };

            return View(model);
        }

        // Captain invites a member
        [HttpPost]
        public async Task<IActionResult> Invite(int teamId, string inviteeUserId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
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
        public async Task<IActionResult> AcceptInvite(int inviteId)
        {
            var user = await _userManager.GetUserAsync(User);
            var invite = await data.CommunityInvites.Include(i => i.CommunityTeam).FirstOrDefaultAsync(i => i.Id == inviteId);

            if (invite == null || invite.InviteeUserId != user.Id) return NotFound();
            if (invite.IsAccepted || invite.IsCancelled) return BadRequest();
            if (invite.ExpiresAt.HasValue && invite.ExpiresAt < DateTime.UtcNow) return BadRequest("Invite expired");

            invite.IsAccepted = true;
            invite.AcceptedAt = DateTime.UtcNow;

            // add member (unapproved by moderator yet)
            var member = new CommunityTeamMember
            {
                CommunityTeamId = invite.CommunityTeamId,
                UserId = user.Id,
                Role = CommunityMemberRole.Player,
                IsApproved = false
            };

            data.CommunityTeamMembers.Add(member);
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
        public async Task<IActionResult> ApproveJoinRequest(int requestId)
        {
            var req = await data.CommunityJoinRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null) return NotFound();

            // Add as member (unapproved by moderator yet)
            var member = new CommunityTeamMember
            {
                CommunityTeamId = req.CommunityTeamId,
                UserId = req.UserId,
                Role = CommunityMemberRole.Player,
                IsApproved = false
            };
            data.CommunityTeamMembers.Add(member);

            data.CommunityJoinRequests.Remove(req);
            await data.SaveChangesAsync();

            return RedirectToAction("TeamRequests", new { teamId = req.CommunityTeamId });
        }

        [HttpPost]
        public async Task<IActionResult> RejectJoinRequest(int requestId)
        {
            var req = await data.CommunityJoinRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null) return NotFound();

            data.CommunityJoinRequests.Remove(req);
            await data.SaveChangesAsync();

            return RedirectToAction("TeamRequests", new { teamId = req.CommunityTeamId });
        }

        // Moderator approves a team
        [Authorize(Roles = "Administrator,Moderator")]
        [HttpPost]
        public async Task<IActionResult> ApproveTeam(int teamId)
        {
            var team = await data.CommunityTeams.Include(t => t.Members).ThenInclude(m => m.User).FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return NotFound();

            team.IsApproved = true;
            team.ApprovedAt = DateTime.UtcNow;
            team.ApprovedBy = User.Identity.Name;

            await data.SaveChangesAsync();

            // Optionally auto-approve the captain and members (or leave member approval separate)
            return Ok(new { success = true });
        }

        // Moderator approves a member (promote their community user to official Player)
        [Authorize(Roles = "Administrator,Moderator")]
        [HttpPost]
        public async Task<IActionResult> ApproveMember(int teamId, string userId)
        {
            var member = await data.CommunityTeamMembers.Include(m => m.User).FirstOrDefaultAsync(m => m.CommunityTeamId == teamId && m.UserId == userId);
            if (member == null) return NotFound();

            member.IsApproved = true;
            member.ApprovedAt = DateTime.UtcNow;
            member.ApprovedBy = User.Identity.Name;

            await data.SaveChangesAsync();

            return Ok(new { success = true });
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
                // If there's already a Player record linked to that user (you can keep mapping in User -> Player id via a claim or table),
                // we must decide how to detect. For simplicity: check Players table by nickname matching user's username.
                var existingPlayer = await data.Players.FirstOrDefaultAsync(p => p.Nickname == member.User.UserName);
                if (existingPlayer == null)
                {
                    var newPlayer = new Player
                    {
                        Nickname = member.User.UserName,
                        FirstName = member.User.FirstName ?? member.User.UserName,
                        LastName = member.User.LastName ?? "",
                        NationalityId = member.User.NationalityId != 0 ? member.User.NationalityId : /* fallback */ 1,
                        BirthDate = DateTime.UtcNow // optional
                    };
                    data.Players.Add(newPlayer);
                    await data.SaveChangesAsync();
                    existingPlayer = newPlayer;
                }

                // create official transfer into the new team at current datetime (or use the community transfer date)
                var pw = new PlayerTeamTransfer
                {
                    PlayerId = existingPlayer.Id,
                    TeamId = officialTeam.Id,
                    TransferDate = DateTime.UtcNow,
                    PositionId = ct.Transfers.FirstOrDefault(t => t.UserId == member.UserId)?.PositionId ?? 0
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
