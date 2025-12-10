using Balkana.Data.Models;
using Balkana.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Balkana.Models.Article;
using Balkana.Services.Images;
using System.IO;

namespace Balkana.Controllers
{
    [Authorize(Roles = "Author, Editor, Moderator,Administrator")]
    public class ArticleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ArticleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // List published articles (for everyone)
        [AllowAnonymous]
        public IActionResult Index()
        {
            var articles = _context.Articles
                .Where(a => a.Status == "Published")
                .Include(a => a.Author)
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

        [Authorize(Roles = "Editor,Administrator")]
        public async Task<IActionResult> Drafts()
        {
            var articles = _context.Articles
                .Where(c=>c.Status!="Published")
                .Include(a => a.Author)
                .ToList();
            return View(articles);
        }

        // Create article (Author or above)
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(ArticleFormModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            model.AuthorId = user.Id;
            model.Status = "Draft";

            string? logoPath = null;

            if (model.File != null && model.File.Length > 0)
            {
                try
                {
                    logoPath = await ImageOptimizer.SaveWebpAsync(
                        model.File,
                        _env.WebRootPath,
                        Path.Combine("uploads", "News", "Thumbnails"),
                        maxWidth: 1280,
                        maxHeight: 720,
                        quality: 85);
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine("❌ IO Exception while saving file: " + ioEx);
                    ModelState.AddModelError("", "File write error: " + ioEx.Message);
                    return View(model);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ General Exception while saving file: " + ex);
                    ModelState.AddModelError("", "Unexpected error while saving file.");
                    return View(model);
                }
            }

            var article = new Article
            {
                Title = model.Title,
                Content = model.Content,
                ThumbnailUrl = logoPath ?? model.ThumbnailUrl,
                CreatedAt = DateTime.UtcNow,
                Status = "Draft",
                AuthorId = user.Id
            };

            Console.WriteLine("Article created with ID = " + article);

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyArticles");
        }

        // GET: Article/Edit/5
        [HttpGet]
        [Authorize(Roles = "Author,Editor,Moderator,Administrator")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var article = await _context.Articles.FindAsync(id);

            if (article == null)
            {
                return NotFound();
            }

            // Only allow the Author or Moderator/Admin to edit
            if (article.AuthorId != user.Id && !User.IsInRole("Moderator") && !User.IsInRole("Administrator") && !User.IsInRole("Editor"))
            {
                return Forbid();
            }

            var ar = new ArticleFormModel
            {
                Title = article.Title,
                Content = article.Content,
                ThumbnailUrl = article.ThumbnailUrl,
                AuthorId = article.AuthorId,
                Status = article.Status
            };

            return View(ar);
        }

        // POST: Article/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Author,Editor,Moderator,Administrator")]
        public async Task<IActionResult> Edit(int id, ArticleFormModel model)
        {
            var articleId = _context.Articles.FirstOrDefault(c=>c.Id==id);
            if (articleId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var article = await _context.Articles.FindAsync(id);

            if (article == null)
            {
                return NotFound();
            }

            // Only allow the Author or Moderator/Admin to edit
            if (article.AuthorId != user.Id && !User.IsInRole("Moderator") && !User.IsInRole("Administrator") && !User.IsInRole("Editor"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                article.Title = model.Title;
                article.Content = model.Content;

                string? logoPath = null;

                if (model.File != null && model.File.Length > 0)
                {
                    try
                    {
                        logoPath = await ImageOptimizer.SaveWebpAsync(
                            model.File,
                            _env.WebRootPath,
                            Path.Combine("uploads", "News", "Thumbnails"),
                            maxWidth: 1280,
                            maxHeight: 720,
                            quality: 85);
                    }
                    catch (IOException ioEx)
                    {
                        Console.WriteLine("❌ IO Exception while saving file: " + ioEx);
                        ModelState.AddModelError("", "File write error: " + ioEx.Message);
                        return View(model);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("❌ General Exception while saving file: " + ex);
                        ModelState.AddModelError("", "Unexpected error while saving file.");
                        return View(model);
                    }
                }

                article.ThumbnailUrl = logoPath ?? model.ThumbnailUrl;


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
            if (article.Status != "Published")
            {
                var user = await _userManager.GetUserAsync(User);
                var isPrivileged =
                    User.IsInRole("Editor") ||
                    User.IsInRole("Administrator") ||
                    (user != null && article.AuthorId == user.Id);

                if (!isPrivileged)
                {
                    return Forbid();
                }
            }

            // Set SEO metadata
            ViewData["Title"] = article.Title;
            var description = article.Content.Length > 160 
                ? System.Text.RegularExpressions.Regex.Replace(article.Content, "<.*?>", string.Empty).Substring(0, 160) + "..."
                : System.Text.RegularExpressions.Regex.Replace(article.Content, "<.*?>", string.Empty);
            ViewData["Description"] = description;
            ViewData["Keywords"] = $"Balkana, esports, {article.Title}, gaming news, tournament news";

            return View(article);
        }

        // Publish (Moderator or Admin)
        [Authorize(Roles = "Editor,Administrator")]
        public async Task<IActionResult> Publish(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article != null)
            {
                article.Status = "Published";
                article.PublishedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Drafts", "Article");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Editor")]
        public async Task<IActionResult> Unpublish(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
            {
                return NotFound();
            }

            article.Status = "Draft";
            article.PublishedAt = null;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Drafts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Editor")]
        public async Task<IActionResult> Delete(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
            {
                return NotFound();
            }

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Drafts));
        }
    }
}
