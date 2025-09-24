using Balkana.Data.Models;
using Balkana.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Balkana.Models.Article;

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
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "News", "Thumbnails");
                Directory.CreateDirectory(uploadsFolder);

                var ext = Path.GetExtension(model.File.FileName);
                var finalFileName = $"{Guid.NewGuid()}{ext}";
                var finalPath = Path.Combine(uploadsFolder, finalFileName);

                // write to temp first
                var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);

                try
                {
                    Console.WriteLine($">>> Writing upload to temp: {tempFile}");
                    await using (var tempStream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await model.File.CopyToAsync(tempStream);
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

                    logoPath = $"/uploads/News/Thumbnails/{finalFileName}";
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
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "News", "Thumbnails");
                    Directory.CreateDirectory(uploadsFolder);

                    var ext = Path.GetExtension(model.File.FileName);
                    var finalFileName = $"{Guid.NewGuid()}{ext}";
                    var finalPath = Path.Combine(uploadsFolder, finalFileName);

                    // write to temp first
                    var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);

                    try
                    {
                        Console.WriteLine($">>> Writing upload to temp: {tempFile}");
                        await using (var tempStream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                        {
                            await model.File.CopyToAsync(tempStream);
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

                        logoPath = $"/uploads/News/Thumbnails/{finalFileName}";
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
            if (article.Status != "Published" && !User.IsInRole("Moderator") && !User.IsInRole("Administrator") && !User.IsInRole("Editor"))
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
    }
}
