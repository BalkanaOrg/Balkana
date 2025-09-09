using Balkana.Data.Models;
using Balkana.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Controllers
{
    [Authorize(Roles = "Author,Moderator,Administrator")]
    public class ArticleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ArticleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // List published articles (for everyone)
        [AllowAnonymous]
        public IActionResult Index()
        {
            var articles = _context.Articles
                .Where(a => a.Status == "Published")
                .OrderByDescending(a => a.PublishedAt)
                .ToList();

            return View(articles);
        }

        // Author dashboard
        public async Task<IActionResult> MyArticles()
        {
            var user = await _userManager.GetUserAsync(User);
            var articles = _context.Articles
                .Where(a => a.AuthorId == user.Id)
                .ToList();

            return View(articles);
        }

        // Create article (Author or above)
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Article model)
        {
            var user = await _userManager.GetUserAsync(User);
            model.AuthorId = user.Id;
            model.Status = "Draft";

            _context.Articles.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyArticles");
        }

        // GET: Article/Edit/5
        [HttpGet]
        [Authorize(Roles = "Author,Moderator,Administrator")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var article = await _context.Articles.FindAsync(id);

            if (article == null)
            {
                return NotFound();
            }

            // Only allow the Author or Moderator/Admin to edit
            if (article.AuthorId != user.Id && !User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            return View(article);
        }

        // POST: Article/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Author,Moderator,Administrator")]
        public async Task<IActionResult> Edit(int id, Article model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var user = await _userManager.GetUserAsync(User);
            var article = await _context.Articles.FindAsync(id);

            if (article == null)
            {
                return NotFound();
            }

            // Only allow the Author or Moderator/Admin to edit
            if (article.AuthorId != user.Id && !User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                article.Title = model.Title;
                article.Content = model.Content;

                // If the article is already published, keep status, otherwise keep as draft
                if (article.Status == "Draft")
                {
                    article.Status = "Draft";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyArticles));
            }

            return View(model);
        }

        // GET: Article/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }

            // Only show published articles publicly
            if (article.Status != "Published" && !User.IsInRole("Moderator") && !User.IsInRole("Administrator"))
            {
                // Authors can view their own drafts
                var user = await _userManager.GetUserAsync(User);
                if (user == null || article.AuthorId != user.Id)
                {
                    return Forbid();
                }
            }

            return View(article);
        }

        // Publish (Moderator or Admin)
        [Authorize(Roles = "Moderator,Administrator")]
        public async Task<IActionResult> Publish(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article != null)
            {
                article.Status = "Published";
                article.PublishedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
