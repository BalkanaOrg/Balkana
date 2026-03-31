using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Balkana.Controllers
{
    public class BalkanaAwardsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BalkanaAwardsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("BalkanaAwards/{year:int}")]
        public async Task<IActionResult> Index(int year)
        {
            var evt = await _context.BalkanaAwards
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Year == year);

            if (evt == null)
            {
                return NotFound();
            }

            var categories = await _context.BalkanaAwardCategories
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            var vm = new Balkana.Models.BalkanaAwards.BalkanaAwardsVotingPageViewModel
            {
                Year = year,
                VotingOpensAt = evt.VotingOpensAt,
                VotingClosesAt = evt.VotingClosesAt,
                Categories = categories.Select(c => new Balkana.Models.BalkanaAwards.BalkanaAwardsCategoryViewModel
                {
                    Id = c.Id,
                    Key = c.Key,
                    Name = c.Name,
                    TargetType = c.TargetType,
                    IsCommunityVoted = c.IsCommunityVoted,
                    IsRanked = c.IsRanked,
                    MaxRanks = c.MaxRanks
                }).ToList()
            };

            return View("BalkanaAwards/Index", vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("BalkanaAwards/{year:int}/vote")]
        public async Task<IActionResult> Vote(int year, Balkana.Models.BalkanaAwards.SubmitVoteRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var evt = await _context.BalkanaAwards.FirstOrDefaultAsync(e => e.Year == year);
            if (evt == null)
                return NotFound();

            var now = DateTime.UtcNow;
            if (evt.VotingOpensAt.HasValue && now < evt.VotingOpensAt.Value)
                return BadRequest("Voting is not open yet.");
            if (evt.VotingClosesAt.HasValue && now > evt.VotingClosesAt.Value)
                return BadRequest("Voting is closed.");

            var category = await _context.BalkanaAwardCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CategoryId);
            if (category == null)
                return NotFound();

            if (!category.IsCommunityVoted)
                return BadRequest("This category is not community-voted.");

            if (request.Items == null)
                request.Items = new List<Balkana.Models.BalkanaAwards.SubmitVoteItem>();

            if (request.Items.Count == 0)
                return BadRequest("No vote items supplied.");

            if (request.Items.Count > Math.Max(1, category.MaxRanks))
                return BadRequest("Too many items supplied.");

            // Basic rank validation
            var ranks = request.Items.Select(i => i.Rank).ToList();
            if (ranks.Any(r => r <= 0) || ranks.Distinct().Count() != ranks.Count)
                return BadRequest("Invalid ranks.");

            // Validate candidate type selection matches category.TargetType
            bool invalid = category.TargetType switch
            {
                "Player" => request.Items.Any(i => i.PlayerId == null || i.TeamId != null || i.TournamentId != null || i.CandidateUserId != null),
                "Team" => request.Items.Any(i => i.TeamId == null || i.PlayerId != null || i.TournamentId != null || i.CandidateUserId != null),
                "Tournament" => request.Items.Any(i => i.TournamentId == null || i.PlayerId != null || i.TeamId != null || i.CandidateUserId != null),
                "User" => request.Items.Any(i => string.IsNullOrWhiteSpace(i.CandidateUserId) || i.PlayerId != null || i.TeamId != null || i.TournamentId != null),
                _ => true
            };
            if (invalid)
                return BadRequest("Invalid vote items for this category.");

            // Eligibility enforcement for Entry/AWP/IGL (player categories with eligibility lists)
            if (category.TargetType == "Player" && (category.Key == "entry_frg" || category.Key == "awper" || category.Key == "igl"))
            {
                var allowed = await _context.BalkanaAwardEligibilityPlayers
                    .AsNoTracking()
                    .Where(x => x.BalkanaAwardsId == evt.Id && x.CategoryId == category.Id)
                    .Select(x => x.PlayerId)
                    .ToListAsync();

                var allowedSet = allowed.ToHashSet();
                if (request.Items.Any(i => i.PlayerId == null || !allowedSet.Contains(i.PlayerId.Value)))
                    return BadRequest("One or more selected players are not eligible.");
            }

            var ballot = await _context.UserVoting
                .Include(v => v.Items)
                .FirstOrDefaultAsync(v => v.BalkanaAwardsId == evt.Id && v.CategoryId == category.Id && v.UserId == userId);

            if (ballot == null)
            {
                ballot = new UserVoting
                {
                    BalkanaAwardsId = evt.Id,
                    CategoryId = category.Id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserVoting.Add(ballot);
            }
            else
            {
                _context.UserVotingItems.RemoveRange(ballot.Items);
                ballot.Items.Clear();
                ballot.CreatedAt = DateTime.UtcNow;
            }

            foreach (var item in request.Items.OrderBy(i => i.Rank))
            {
                ballot.Items.Add(new UserVotingItem
                {
                    Rank = item.Rank,
                    PlayerId = item.PlayerId,
                    TeamId = item.TeamId,
                    TournamentId = item.TournamentId,
                    CandidateUserId = item.CandidateUserId
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your vote has been saved.";
            return RedirectToAction(nameof(Index), new { year });
        }
    }
}

