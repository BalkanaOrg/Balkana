using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;

namespace Balkana.Controllers
{
    public class SitemapController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public SitemapController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/sitemap.xml", Name = "SitemapXml")]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<IActionResult> Index()
        {
            try
            {
                Console.WriteLine("SitemapController.Index() called!");
                
                // Try to get BaseUrl from configuration (supports appsettings.json, environment variables, Docker env vars)
                var baseUrl = _configuration["BaseUrl"] 
                             ?? Environment.GetEnvironmentVariable("BaseUrl")
                             ?? $"{Request.Scheme}://{Request.Host}";
                
                Console.WriteLine($"Sitemap requested. BaseUrl: {baseUrl}");
                Debug.WriteLine($"Sitemap requested. BaseUrl: {baseUrl}");

                Console.WriteLine("Sitemap: Fetching data from database...");
                
                // Fetch all data first
                var articles = await GetArticleUrls(baseUrl);
                var tournaments = await GetTournamentUrls(baseUrl);
                var teams = await GetTeamUrls(baseUrl);
                var players = await GetPlayerUrls(baseUrl);

                Console.WriteLine($"Sitemap: Found {articles.Length} articles, {tournaments.Length} tournaments, {teams.Length} teams, {players.Length} players");

                // Build URL elements list
                var urlElements = new List<XElement>
                {
                    // Home page
                    CreateUrlElement(baseUrl, "/", DateTime.UtcNow, "daily", 1.0),

                    // Static pages
                    CreateUrlElement(baseUrl, "/Tournaments", DateTime.UtcNow, "weekly", 0.9),
                    CreateUrlElement(baseUrl, "/Article", DateTime.UtcNow, "daily", 0.9),
                    CreateUrlElement(baseUrl, "/Teams", DateTime.UtcNow, "weekly", 0.8),
                    CreateUrlElement(baseUrl, "/Players", DateTime.UtcNow, "weekly", 0.8),
                    CreateUrlElement(baseUrl, "/Store", DateTime.UtcNow, "weekly", 0.7)
                };

                // Add dynamic content
                urlElements.AddRange(articles);
                urlElements.AddRange(tournaments);
                urlElements.AddRange(teams);
                urlElements.AddRange(players);

                // Create urlset element with proper namespace
                var sitemapNs = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
                var urlset = new XElement(sitemapNs + "urlset",
                    urlElements.ToArray()
                );
                
                // Add namespace declarations using proper XNamespace syntax
                urlset.Add(new XAttribute(XNamespace.Xmlns + "news", "http://www.google.com/schemas/sitemap-news/0.9"));
                urlset.Add(new XAttribute(XNamespace.Xmlns + "xhtml", "http://www.w3.org/1999/xhtml"));
                urlset.Add(new XAttribute(XNamespace.Xmlns + "image", "http://www.google.com/schemas/sitemap-image/1.1"));
                urlset.Add(new XAttribute(XNamespace.Xmlns + "video", "http://www.google.com/schemas/sitemap-video/1.1"));
                
                var sitemap = new XDocument(
                    new XDeclaration("1.0", "UTF-8", "yes"),
                    urlset
                );

                Console.WriteLine("Sitemap: Generated XML, returning response...");
                return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // Log error with full details
                Console.WriteLine($"Sitemap ERROR: {ex.Message}");
                Console.WriteLine($"Sitemap ERROR StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Sitemap ERROR InnerException: {ex.InnerException.Message}");
                }
                
                // Return a basic sitemap on error instead of throwing
                try
                {
                    var baseUrl = _configuration["BaseUrl"] 
                                 ?? Environment.GetEnvironmentVariable("BaseUrl")
                                 ?? $"{Request.Scheme}://{Request.Host}";
                    
                    var errorSitemap = new XDocument(
                        new XDeclaration("1.0", "UTF-8", "yes"),
                        new XElement("urlset",
                            new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                            CreateUrlElement(baseUrl, "/", DateTime.UtcNow, "daily", 1.0)
                        )
                    );
                    
                    return Content(errorSitemap.ToString(), "application/xml", Encoding.UTF8);
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"Sitemap ERROR in error handler: {ex2.Message}");
                    // Return minimal XML
                    return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"><url><loc>" + 
                        (Request.Scheme + "://" + Request.Host) + "</loc></url></urlset>", 
                        "application/xml", Encoding.UTF8);
                }
            }
        }

        private XElement CreateUrlElement(string baseUrl, string path, DateTime lastMod, string changeFreq, double priority)
        {
            // Ensure path is properly formatted
            var fullUrl = $"{baseUrl}{path}";
            
            // Use sitemap namespace for all elements
            var sitemapNs = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
            
            return new XElement(sitemapNs + "url",
                new XElement(sitemapNs + "loc", fullUrl), // XElement automatically encodes content
                new XElement(sitemapNs + "lastmod", lastMod.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                new XElement(sitemapNs + "changefreq", changeFreq),
                new XElement(sitemapNs + "priority", priority.ToString("F1"))
            );
        }

        private async Task<XElement[]> GetArticleUrls(string baseUrl)
        {
            var articles = await _context.Articles
                .Where(a => a.Status == "Published" && a.PublishedAt.HasValue)
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            return articles.Select(article => 
                CreateUrlElement(
                    baseUrl, 
                    $"/Article/Details/{article.Id}", 
                    article.PublishedAt!.Value, 
                    "monthly", 
                    0.8
                )
            ).ToArray();
        }

        private async Task<XElement[]> GetTournamentUrls(string baseUrl)
        {
            var tournaments = await _context.Tournaments
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            return tournaments.Select(tournament =>
            {
                var lastMod = tournament.EndDate > DateTime.UtcNow 
                    ? tournament.StartDate 
                    : tournament.EndDate;
                
                return CreateUrlElement(
                    baseUrl,
                    $"/Tournaments/Details/{tournament.Id}",
                    lastMod,
                    "weekly",
                    0.9
                );
            }).ToArray();
        }

        private async Task<XElement[]> GetTeamUrls(string baseUrl)
        {
            var teams = await _context.Teams
                .ToListAsync();

            return teams.Select(team =>
                CreateUrlElement(
                    baseUrl,
                    $"/Teams/Details/{team.Id}",
                    DateTime.UtcNow,
                    "monthly",
                    0.7
                )
            ).ToArray();
        }

        private async Task<XElement[]> GetPlayerUrls(string baseUrl)
        {
            var players = await _context.Players
                .ToListAsync();

            return players.Select(player =>
                CreateUrlElement(
                    baseUrl,
                    $"/Players/Details/{player.Id}",
                    DateTime.UtcNow,
                    "monthly",
                    0.7
                )
            ).ToArray();
        }
    }
}

