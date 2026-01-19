using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Data.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SearchController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery] string query, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Ok(new { teams = new List<object>(), players = new List<object>(), tournaments = new List<object>(), articles = new List<object>() });
            }

            var searchTerm = query.ToLower().Trim();
            var searchLimit = Math.Min(limit, 10); // Max 10 per category

            var teams = await _context.Teams
                .Where(t => t.FullName.ToLower().Contains(searchTerm) || t.Tag.ToLower().Contains(searchTerm))
                .Include(t => t.Game)
                .Take(searchLimit)
                .ToListAsync();

            var teamsResult = teams.Select(t => new
            {
                id = t.Id,
                name = t.FullName,
                tag = t.Tag,
                type = "team",
                game = t.Game.FullName,
                url = $"/Teams/Details/{t.Id}?information={Uri.EscapeDataString(t.GetInformation())}"
            }).ToList();

            var players = await _context.Players
                .Where(p => p.Nickname.ToLower().Contains(searchTerm) ||
                           (p.FirstName != null && p.FirstName.ToLower().Contains(searchTerm)) ||
                           (p.LastName != null && p.LastName.ToLower().Contains(searchTerm)))
                .Take(searchLimit)
                .ToListAsync();

            var playersResult = players.Select(p => new
            {
                id = p.Id,
                name = p.Nickname,
                fullName = (p.FirstName != null || p.LastName != null) ? $"{p.FirstName} {p.LastName}".Trim() : null,
                type = "player",
                url = $"/Players/Profile/{p.Id}?information={Uri.EscapeDataString(p.GetInformation())}"
            }).ToList();

            var tournaments = await _context.Tournaments
                .Where(t => t.FullName.ToLower().Contains(searchTerm) || t.ShortName.ToLower().Contains(searchTerm))
                .Include(t => t.Game)
                .Take(searchLimit)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.FullName,
                    shortName = t.ShortName,
                    type = "tournament",
                    game = t.Game.FullName,
                    url = $"/Tournaments/Details/{t.Id}"
                })
                .ToListAsync();

            var articles = await _context.Articles
                .Where(a => a.Status == "Published" && a.Title.ToLower().Contains(searchTerm))
                .Include(a => a.Author)
                .Take(searchLimit)
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Title,
                    type = "article",
                    url = $"/Article/Details/{a.Id}",
                    publishedAt = a.PublishedAt
                })
                .ToListAsync();

            return Ok(new
            {
                teams = teamsResult,
                players = playersResult,
                tournaments,
                articles
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Ok(new List<object>());
            }

            var searchTerm = query.ToLower().Trim();
            var searchLimit = Math.Min(limit, 10);

            var users = await _userManager.Users
                .Where(u => u.UserName.ToLower().Contains(searchTerm) ||
                           (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                           (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)) ||
                           u.Email.ToLower().Contains(searchTerm))
                .Take(searchLimit)
                .Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    email = u.Email
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}

