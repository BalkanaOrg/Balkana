using AutoMapper;
using AutoMapper.QueryableExtensions;
using Balkana.Data;
using Balkana.Data.DTOs.Bracket;
using Balkana.Data.Models;
using Balkana.Models.Tournaments;
using Balkana.Services.Bracket;
using Balkana.Services.Teams.Models;
using Balkana.Services.Tournaments;
using Balkana.Services.Tournaments.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PuppeteerSharp;
using Balkana.Services.Images;
using System.IO;

namespace Balkana.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DoubleEliminationBracketService bracketService;
        private readonly IMapper mapper;
        private readonly AutoMapper.IConfigurationProvider con;
        private readonly IWebHostEnvironment env;
        //private readonly ITournamentService tournaments;

        public TournamentsController(ApplicationDbContext context, DoubleEliminationBracketService bracketService, IMapper mapper, IWebHostEnvironment env)
        {
            _context = context;
            this.bracketService = bracketService;
            //this.tournaments = tournaments;
            this.mapper = mapper;
            this.con= mapper.ConfigurationProvider;
            this.env = env;
        }

        // GET: Tournament/Index
        public async Task<IActionResult> Index()
        {
            var tournaments = await _context.Tournaments
                .Include(t => t.Organizer)
                .Include(t => t.Game)
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                .OrderByDescending(c=>c.StartDate)
                .ToListAsync();

            return View(tournaments);
        }

        // GET: Tournament/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Organizer)
                .Include(t => t.Game)
                .Include(t => t.TournamentTeams).ThenInclude(tt => tt.Team) // include teams here
                .Include(t => t.Series).ThenInclude(s => s.TeamA)
                .Include(t => t.Series).ThenInclude(s => s.TeamB)
                .Include(t => t.Series).ThenInclude(s => s.NextSeries)
                .Include(t => t.Placements).ThenInclude(p => p.Team)
                .FirstOrDefaultAsync(t => t.Id == id);



            if (tournament == null) return NotFound();   // move this up

            ViewData["OrderedSeries"] = tournament.Series
                .OrderBy(s => s.Bracket)
                .ThenBy(s => s.Round)
                .ThenBy(s => s.Position)
                .ToList();

            var participatingTeams = await _context.TournamentTeams
                .Where(tt => tt.TournamentId == id)
                .Include(tt => tt.Team)
                    .ThenInclude(t => t.Transfers)
                        .ThenInclude(tr => tr.Player)
                .OrderBy(tt => tt.Seed)
                .Select(tt => new TournamentTeamRosterViewModel
                {
                    Team = tt.Team,
                    Players = tt.Team.Transfers
                    .Where(tr =>
                        tr.StartDate <= tournament.StartDate &&
                        (tr.EndDate == null || tr.EndDate >= tournament.StartDate) &&
                        tr.Status == PlayerTeamStatus.Active)
                    .GroupBy(tr => tr.PlayerId)
                    .Select(g => g.OrderByDescending(tr => tr.StartDate).First().Player)
                    .ToList()
                })
                .ToListAsync();

            ViewData["ParticipatingTeams"] = participatingTeams;

            // Set SEO metadata
            ViewData["Title"] = tournament.FullName;
            ViewData["Description"] = tournament.Description?.Length > 160 
                ? tournament.Description.Substring(0, 160) + "..." 
                : tournament.Description ?? $"{tournament.FullName} - {tournament.Game?.FullName} tournament";
            ViewData["Keywords"] = $"Balkana, {tournament.FullName}, {tournament.Game?.FullName}, esports tournament, {tournament.Organizer?.FullName}";

            return View(tournament);
        }

        // GET: Tournament/Add
        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Add() 
        {
            var games = AllGames();
            var organizers = AllOrganizers();
            var eliminationTypes = AllEliminationTypes();

            var vm = new TournamentFormViewModel
            {
                Games = games,
                Organizers = organizers,
                EliminationTypes = eliminationTypes
            };
            return View(vm);
        } 

        // POST: Tournament/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Add(TournamentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var vm = new TournamentFormViewModel
                {
                    Games = AllGames(),
                    Organizers = AllOrganizers(),
                    EliminationTypes = AllEliminationTypes()
                };
                return View(vm);
            }

            string? logoPath = null;

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                try
                {
                    logoPath = await ImageOptimizer.SaveWebpAsync(
                        model.LogoFile,
                        env.WebRootPath,
                        Path.Combine("uploads", "Tournaments"),
                        maxWidth: 1920,
                        maxHeight: 1080,
                        quality: 85);
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine("❌ IO Exception while saving file: " + ioEx);
                    ModelState.AddModelError("", "File write error: " + ioEx.Message);
                    var vm = new TournamentFormViewModel
                    {
                        Games = AllGames(),
                        Organizers = AllOrganizers(),
                        EliminationTypes = AllEliminationTypes()
                    };
                    return View(vm);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ General Exception while saving file: " + ex);
                    ModelState.AddModelError("", "Unexpected error while saving file.");
                    var vm = new TournamentFormViewModel
                    {
                        Games = AllGames(),
                        Organizers = AllOrganizers(),
                        EliminationTypes = AllEliminationTypes()
                    };
                    return View(vm);
                }
            }

            var tournament = new Tournament
            {
                FullName = model.FullName,
                ShortName = model.ShortName,
                OrganizerId = model.OrganizerId,
                Description = model.Description,
                StartDate = model.StartDate,
                BannerUrl = logoPath,
                Elimination = model.EliminationType,
                EndDate = model.EndDate,
                GameId = model.GameId,
                PrizePool = model.PrizePool,
                PointsConfiguration = model.PointsConfiguration,
                PrizeConfiguration = model.PrizeConfiguration
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Edit(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();

            var vm = new TournamentFormViewModel
            {
                Id = tournament.Id,
                FullName = tournament.FullName,
                ShortName = tournament.ShortName,
                OrganizerId = tournament.OrganizerId,
                Description = tournament.Description,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                PrizePool = tournament.PrizePool,
                PointsConfiguration = tournament.PointsConfiguration,
                PrizeConfiguration = tournament.PrizeConfiguration,
                BannerUrl = tournament.BannerUrl,
                EliminationType = tournament.Elimination,
                GameId = tournament.GameId,
                Games = AllGames(),
                Organizers = AllOrganizers(),
                EliminationTypes = AllEliminationTypes(),
                AvailableTeams = await _context.Teams
                    .Select(t => new TeamSelectItem { Id = t.Id, FullName = t.FullName })
                    .ToListAsync(),
                SelectedTeamIds = tournament.TournamentTeams.Select(tt => tt.TeamId).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Edit(TournamentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Games = AllGames();
                model.Organizers = AllOrganizers();
                model.EliminationTypes = AllEliminationTypes();
                model.AvailableTeams = await _context.Teams
                    .Select(t => new TeamSelectItem { Id = t.Id, FullName = t.FullName })
                    .ToListAsync();
                return View(model);
            }

            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams)
                .FirstOrDefaultAsync(t => t.Id == model.Id);

            if (tournament == null) return NotFound();

            // Update basic fields
            tournament.FullName = model.FullName;
            tournament.ShortName = model.ShortName;
            tournament.OrganizerId = model.OrganizerId;
            tournament.Description = model.Description;
            tournament.StartDate = model.StartDate;
            tournament.EndDate = model.EndDate;
            tournament.PrizePool = model.PrizePool;
            tournament.Elimination = model.EliminationType;
            tournament.GameId = model.GameId;
            tournament.PointsConfiguration = model.PointsConfiguration;
            tournament.PrizeConfiguration = model.PrizeConfiguration;

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                try
                {
                    tournament.BannerUrl = await ImageOptimizer.SaveWebpAsync(
                        model.LogoFile,
                        env.WebRootPath,
                        Path.Combine("uploads", "Tournaments"),
                        maxWidth: 1920,
                        maxHeight: 1080,
                        quality: 85);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving banner: {ex.Message}");
                    model.Games = AllGames();
                    model.Organizers = AllOrganizers();
                    model.EliminationTypes = AllEliminationTypes();
                    model.AvailableTeams = await _context.Teams
                        .Select(t => new TeamSelectItem { Id = t.Id, FullName = t.FullName })
                        .ToListAsync();
                    return View(model);
                }
            }

            // ✅ Update participating teams
            _context.TournamentTeams.RemoveRange(tournament.TournamentTeams);

            var newTeams = model.SelectedTeamIds
                .Select((teamId, index) => new TournamentTeam
                {
                    TournamentId = tournament.Id,
                    TeamId = teamId,
                    Seed = index + 1
                });

            await _context.TournamentTeams.AddRangeAsync(newTeams);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = tournament.Id });
        }



        [HttpGet("api/tournaments/{id}/bracket")]
        public IActionResult GetBracket(int id)
        {
            // Fetch tournament with teams and series
            var tournament = _context.Tournaments
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                .Include(t => t.Series)
                    .ThenInclude(s => s.TeamA)
                .Include(t => t.Series)
                    .ThenInclude(s => s.TeamB)
                .Include(t => t.Series)
                    .ThenInclude(s => s.Matches)
                .FirstOrDefault(t => t.Id == id);

            if (tournament == null)
                return NotFound();

            // Participants
            var participants = tournament.TournamentTeams
            .OrderBy(tt => tt.Seed)
            .Select(tt => new
            {
                id = tt.Team.Id,
                name = tt.Team.FullName
            })
            .ToList();

            // Add dummy participant for TBD slots
            participants.Add(new { id = -1, name = "TBD" });

            // Stage ID
            var stageId = 1;

            // Matches - show all matches including TBD vs TBD
            var matches = tournament.Series
                .Select(s => new
                {
                    id = s.Id,
                    series_id = s.Id, // Add series ID for easier mapping
                    stage_id = 1, // single stage for simplicity
                    group_id = s.Bracket == BracketType.Upper ? 0 :
                   s.Bracket == BracketType.Lower ? 1 : 2,
                    round_id = s.Round,
                    number = s.Position,
                    status = s.isFinished ? "finished" : "open",
                    opponent1 = s.TeamA != null
                        ? new { 
                            id = s.TeamA.Id, 
                            name = s.TeamA.FullName, 
                            score = GetTeamScore(s, s.TeamA), 
                            result = GetTeamResult(s, s.TeamA) 
                        }
                        : new { id = -1, name = "TBD", score = 0, result = "" },
                    opponent2 = s.TeamB != null
                        ? new { 
                            id = s.TeamB.Id, 
                            name = s.TeamB.FullName, 
                            score = GetTeamScore(s, s.TeamB), 
                            result = GetTeamResult(s, s.TeamB) 
                        }
                        : new { id = -1, name = "TBD", score = 0, result = "" }
                }).ToList();

            // Stages - must include skipFirstRound and groups
            var stages = new[]
            {
                new
                {
                    id = stageId,
                    name = tournament.Elimination == EliminationType.Single ? "Single Elimination" : "Double Elimination",
                    type = tournament.Elimination == EliminationType.Single ? "single_elimination" : "double_elimination",
                    settings = new
                    {
                        skipFirstRound = false
                    },
                    groups = tournament.Elimination == EliminationType.Single 
                        ? new[] { new { id = 0, name = "Main Bracket" } }
                        : new[]
                        {
                            new { id = 0, name = "Upper Bracket" },
                            new { id = 1, name = "Lower Bracket" },
                            new { id = 2, name = "Grand Final" }
                        }
                }
            };

            return Ok(new
            {
                participants,
                matches,
                matchGames = new object[0],
                stages
            });
        }

        [HttpGet("api/tournaments/{id}/bracket/image")]
        public async Task<IActionResult> GetBracketImage(int id, [FromQuery] bool html = false)
        {
            // Fetch tournament with teams and series
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                .Include(t => t.Series)
                    .ThenInclude(s => s.TeamA)
                .Include(t => t.Series)
                    .ThenInclude(s => s.TeamB)
                .Include(t => t.Series)
                    .ThenInclude(s => s.Matches)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
                return NotFound("Tournament not found");

            if (!tournament.Series.Any())
                return BadRequest("Bracket not yet generated for this tournament");

            try
            {
                // Always try to capture the actual bracket from the tournament page first
                var imageBytes = await CaptureBracketFromTournamentPage(id);
                
                // Return image with proper headers for Discord
                Response.Headers["Content-Type"] = "image/png";
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                Response.Headers["Content-Disposition"] = "inline";
                
                return File(imageBytes, "image/png", $"{tournament.FullName.Replace(" ", "_")}_bracket.png");
            }
            catch (Exception ex)
            {
                // If image capture fails, return HTML with proper meta tags for Discord embeds
                var bracketData = await GetBracketDataForImage(tournament);
                var htmlContent = GenerateBracketHtmlWithMetaTags(tournament, bracketData, Request);
                return Content(htmlContent, "text/html");
            }
        }

        private async Task<object> GetBracketDataForImage(Tournament tournament)
        {
            // Reuse the existing GetBracket logic
            var participants = tournament.TournamentTeams
                .OrderBy(tt => tt.Seed)
                .Select(tt => new
                {
                    id = tt.Team.Id,
                    name = tt.Team.FullName
                })
                .ToList();

            // Add dummy participant for TBD slots
            participants.Add(new { id = -1, name = "TBD" });

            var matches = tournament.Series
                .Select(s => new
                {
                    id = s.Id,
                    series_id = s.Id,
                    stage_id = 1,
                    group_id = s.Bracket == BracketType.Upper ? 0 :
                               s.Bracket == BracketType.Lower ? 1 : 2,
                    round_id = s.Round,
                    number = s.Position,
                    status = s.isFinished ? "finished" : "open",
                    opponent1 = s.TeamA != null
                        ? new { 
                            id = s.TeamA.Id, 
                            name = s.TeamA.FullName, 
                            score = GetTeamScore(s, s.TeamA), 
                            result = GetTeamResult(s, s.TeamA) 
                        }
                        : new { id = -1, name = "TBD", score = 0, result = "" },
                    opponent2 = s.TeamB != null
                        ? new { 
                            id = s.TeamB.Id, 
                            name = s.TeamB.FullName, 
                            score = GetTeamScore(s, s.TeamB), 
                            result = GetTeamResult(s, s.TeamB) 
                        }
                        : new { id = -1, name = "TBD", score = 0, result = "" }
                }).ToList();

            var stages = new[]
            {
                new
                {
                    id = 1,
                    name = tournament.Elimination == EliminationType.Single ? "Single Elimination" : "Double Elimination",
                    type = tournament.Elimination == EliminationType.Single ? "single_elimination" : "double_elimination",
                    settings = new { skipFirstRound = false },
                    groups = tournament.Elimination == EliminationType.Single 
                        ? new[] { new { id = 0, name = "Main Bracket" } }
                        : new[]
                        {
                            new { id = 0, name = "Upper Bracket" },
                            new { id = 1, name = "Lower Bracket" },
                            new { id = 2, name = "Grand Final" }
                        }
                }
            };

            return new
            {
                participants,
                matches,
                matchGames = new object[0],
                stages
            };
        }

        private string GenerateBracketHtml(Tournament tournament, object bracketData)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{tournament.FullName} - Bracket</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 20px;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
            color: white;
            min-height: 100vh;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .header h1 {{
            font-size: 2.5em;
            margin: 0;
            background: linear-gradient(45deg, #ff6b6b, #4ecdc4);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
        }}
        .bracket-container {{
            background: rgba(255, 255, 255, 0.1);
            border-radius: 15px;
            padding: 20px;
            backdrop-filter: blur(10px);
            border: 1px solid rgba(255, 255, 255, 0.2);
        }}
        #bracket-viewer {{
            width: 100%;
            min-height: 600px;
        }}
    </style>
    <script src='https://cdn.jsdelivr.net/npm/brackets-viewer@1.3.0/dist/brackets-viewer.min.js'></script>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/brackets-viewer@1.3.0/dist/brackets-viewer.min.css'>
</head>
<body>
    <div class='header'>
        <h1>{tournament.FullName}</h1>
        <p>Tournament Bracket</p>
    </div>
    
    <div class='bracket-container'>
        <div id='bracket-viewer'></div>
    </div>

    <script>
        const bracketData = {System.Text.Json.JsonSerializer.Serialize(bracketData)};
        
        const viewer = new BracketViewer.BracketsViewer(
            document.getElementById('bracket-viewer'),
            {{
                participantOriginPlacement: 'before',
                separatedChildCountLabel: true,
                showSlotsOrigin: false,
                showLowerBracketSlotsOrigin: false,
                highlightParticipantOnHover: true,
                showParticipantCountryFlag: false,
                showParticipantImage: false
            }}
        );
        
        viewer.render(bracketData);
    </script>
</body>
</html>";

            return html;
        }

        private string GenerateBracketHtmlWithMetaTags(Tournament tournament, object bracketData, HttpRequest request)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var imageUrl = $"{baseUrl}/api/tournaments/{tournament.Id}/bracket/image";
            
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{tournament.FullName} - Tournament Bracket</title>
    <meta name='description' content='Tournament bracket for {tournament.FullName}'>
    
    <!-- Open Graph / Facebook -->
    <meta property='og:type' content='website'>
    <meta property='og:url' content='{imageUrl}'>
    <meta property='og:title' content='{tournament.FullName} - Tournament Bracket'>
    <meta property='og:description' content='Tournament bracket for {tournament.FullName}'>
    <meta property='og:image' content='{imageUrl}'>
    <meta property='og:image:width' content='1920'>
    <meta property='og:image:height' content='1080'>
    <meta property='og:image:type' content='image/png'>
    
    <!-- Twitter -->
    <meta property='twitter:card' content='summary_large_image'>
    <meta property='twitter:url' content='{imageUrl}'>
    <meta property='twitter:title' content='{tournament.FullName} - Tournament Bracket'>
    <meta property='twitter:description' content='Tournament bracket for {tournament.FullName}'>
    <meta property='twitter:image' content='{imageUrl}'>
    
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 20px;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
            color: white;
            min-height: 100vh;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .header h1 {{
            font-size: 2.5em;
            margin: 0;
            background: linear-gradient(45deg, #ff6b6b, #4ecdc4);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
        }}
        .bracket-container {{
            background: rgba(255, 255, 255, 0.1);
            border-radius: 15px;
            padding: 20px;
            backdrop-filter: blur(10px);
            border: 1px solid rgba(255, 255, 255, 0.2);
        }}
        #bracket-viewer {{
            width: 100%;
            min-height: 600px;
        }}
    </style>
    <script src='https://cdn.jsdelivr.net/npm/brackets-viewer@1.3.0/dist/brackets-viewer.min.js'></script>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/brackets-viewer@1.3.0/dist/brackets-viewer.min.css'>
</head>
<body>
    <div class='header'>
        <h1>{tournament.FullName}</h1>
        <p>Tournament Bracket</p>
    </div>
    
    <div class='bracket-container'>
        <div id='bracket-viewer'></div>
    </div>

    <script>
        const bracketData = {System.Text.Json.JsonSerializer.Serialize(bracketData)};
        
        const viewer = new BracketViewer.BracketsViewer(
            document.getElementById('bracket-viewer'),
            {{
                participantOriginPlacement: 'before',
                separatedChildCountLabel: true,
                showSlotsOrigin: false,
                showLowerBracketSlotsOrigin: false,
                highlightParticipantOnHover: true,
                showParticipantCountryFlag: false,
                showParticipantImage: false
            }}
        );
        
        viewer.render(bracketData);
    </script>
</body>
</html>";

            return html;
        }

        private async Task<byte[]> CaptureBracketFromTournamentPage(int tournamentId)
        {
            // Launch headless browser
            await new BrowserFetcher().DownloadAsync();
            
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });
            
            using var page = await browser.NewPageAsync();
            
            // Set viewport to capture the bracket properly
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 1920,
                Height = 1080,
                DeviceScaleFactor = 2 // Higher DPI for better quality
            });
            
            // Navigate to the tournament details page
            var tournamentUrl = $"{Request.Scheme}://{Request.Host}/Tournaments/Details/{tournamentId}";
            await page.GoToAsync(tournamentUrl, WaitUntilNavigation.Networkidle2);
            
            // Wait for the bracket to load
            await page.WaitForSelectorAsync("#bracket-viewer", new WaitForSelectorOptions { Timeout = 10000 });
            
            // Wait a bit more for any animations or dynamic content to settle
            await Task.Delay(2000);
            
            // Find the bracket container element
            var bracketElement = await page.QuerySelectorAsync("#bracket-viewer");
            if (bracketElement == null)
            {
                throw new Exception("Bracket element not found on page");
            }
            
            // Capture screenshot of just the bracket area
            var screenshot = await bracketElement.ScreenshotDataAsync();
            
            return screenshot;
        }


        [HttpGet("api/series/{id}/details")]
        public IActionResult GetSeriesDetails(int id)
        {
            var series = _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.WinnerTeam)
                .Include(s => s.Tournament)
                .Include(s => s.Matches)
                    .ThenInclude(m => m.WinnerTeam)
                .Include(s => s.Matches)
                    .ThenInclude(m => ((MatchCS)m).Map)
                .FirstOrDefault(s => s.Id == id);

            if (series == null)
                return NotFound();

            // Determine BO format based on series characteristics
            string bestOfFormat = DetermineBestOfFormat(series);

            var seriesData = new
            {
                id = series.Id,
                name = series.Name,
                round = series.Round,
                position = series.Position,
                bracket = series.Bracket.ToString(),
                isFinished = series.isFinished,
                bestOfFormat = bestOfFormat,
                teamA = series.TeamA != null ? new { id = series.TeamA.Id, fullName = series.TeamA.FullName } : null,
                teamB = series.TeamB != null ? new { id = series.TeamB.Id, fullName = series.TeamB.FullName } : null,
                winnerTeam = series.WinnerTeam != null ? new { id = series.WinnerTeam.Id, fullName = series.WinnerTeam.FullName } : null,
                matches = series.Matches.Select(m => new
                {
                    id = m.Id,
                    externalMatchId = m.ExternalMatchId,
                    source = m.Source,
                    playedAt = m.PlayedAt,
                    isCompleted = m.IsCompleted,
                    mapName = m is MatchCS csMatch ? (csMatch.Map?.Name ?? "No Map") : "N/A",
                    winnerTeam = m.WinnerTeam != null ? new { id = m.WinnerTeam.Id, fullName = m.WinnerTeam.FullName } : null
                }).OrderBy(m => m.playedAt).ToList()
            };

            return Ok(seriesData);
        }

        private string DetermineBestOfFormat(Series series)
        {
            // Check if it's a bye match (one team is null)
            if ((series.TeamA == null && series.TeamB != null) || 
                (series.TeamA != null && series.TeamB == null))
            {
                return "Bye";
            }

            // Check if both teams are null (TBD match)
            if (series.TeamA == null && series.TeamB == null)
            {
                return "TBD";
            }

            // If no matches yet, determine based on tournament structure
            if (!series.Matches.Any())
            {
                return DetermineBestOfFromTournamentStructure(series);
            }

            // If matches exist, determine based on actual match count and completion
            var matchCount = series.Matches.Count;
            
            // Debug logging
            Console.WriteLine($"Series {series.Id}: {matchCount} matches, Finished: {series.isFinished}");
            
            if (matchCount == 1)
            {
                return "BO1";
            }
            else if (matchCount == 2)
            {
                // Check if this is a legitimate BO2 or if it's incomplete
                if (series.isFinished)
                {
                    // If finished with 2 matches, it could be a BO3 that ended 2-0
                    var teamAWins = series.Matches.Count(m => m.WinnerTeamId == series.TeamAId);
                    var teamBWins = series.Matches.Count(m => m.WinnerTeamId == series.TeamBId);
                    var maxWins = Math.Max(teamAWins, teamBWins);
                    
                    if (maxWins == 2)
                    {
                        return "BO3 (2-0)";
                    }
                    else
                    {
                        return "BO2 (Invalid)";
                    }
                }
                else
                {
                    // Not finished with 2 matches - likely incomplete BO3
                    return "BO3 (Incomplete)";
                }
            }
            else if (matchCount == 3)
            {
                // For 3 matches, determine if it's BO3 or BO5 based on completion
                if (series.isFinished)
                {
                    var teamAWins = series.Matches.Count(m => m.WinnerTeamId == series.TeamAId);
                    var teamBWins = series.Matches.Count(m => m.WinnerTeamId == series.TeamBId);
                    var maxWins = Math.Max(teamAWins, teamBWins);
                    
                    // If a team won 2 matches, it's a BO3 (2-0 or 2-1)
                    // If a team won 3 matches, it's a BO5 (3-0)
                    if (maxWins == 2)
                    {
                        return "BO3";
                    }
                    else if (maxWins == 3)
                    {
                        return "BO5";
                    }
                }
                
                // Default to BO3 for 3 matches
                return "BO3";
            }
            else if (matchCount == 5)
            {
                return "BO5";
            }
            else
            {
                return $"BO{matchCount}";
            }
        }

        private string DetermineBestOfFromTournamentStructure(Series series)
        {
            // Determine BO format based on tournament round and structure
            // This is a heuristic approach since we don't have explicit BO format in the model
            
            if (series.Round == 1)
            {
                // First round is typically BO1
                return "BO1";
            }
            else if (series.Round == 2)
            {
                // Second round (semifinals) is typically BO3
                return "BO3";
            }
            else if (series.Round >= 3)
            {
                // Finals and later rounds are typically BO5
                return "BO5";
            }
            
            // Default fallback
            return "BO3";
        }

        private int GetTeamScore(Series series, Team team)
        {
            if (!series.isFinished || !series.Matches.Any())
                return 0;

            // Count wins for this team in the series
            int wins = 0;
            foreach (var match in series.Matches)
            {
                if (match.IsCompleted)
                {
                    var winner = DetermineMatchWinner(match);
                    if (winner == team)
                    {
                        wins++;
                    }
                }
            }
            return wins;
        }

        private string GetTeamResult(Series series, Team team)
        {
            if (!series.isFinished)
                return "";

            var teamScore = GetTeamScore(series, team);
            var otherTeam = series.TeamA == team ? series.TeamB : series.TeamA;
            var otherScore = GetTeamScore(series, otherTeam);

            if (teamScore > otherScore)
                return "win";
            else if (teamScore < otherScore)
                return "loss";
            else
                return "draw";
        }

        private Team DetermineMatchWinner(Match match)
        {
            // Use the WinnerTeam directly from the match
            if (match.WinnerTeam != null)
            {
                return match.WinnerTeam;
            }

            // Fallback: determine winner from player statistics if WinnerTeam is not set
            if (match is MatchCS csMatch)
            {
                var teamAStats = csMatch.PlayerStats
                    .Where(ps => ps.Team == csMatch.TeamASourceSlot)
                    .OfType<PlayerStatistic_CS2>()
                    .ToList();
                var teamBStats = csMatch.PlayerStats
                    .Where(ps => ps.Team == csMatch.TeamBSourceSlot)
                    .OfType<PlayerStatistic_CS2>()
                    .ToList();

                if (teamAStats.Any() && teamBStats.Any())
                {
                    // Use rounds played to determine winner as fallback
                    var teamARounds = teamAStats.FirstOrDefault()?.RoundsPlayed ?? 0;
                    var teamBRounds = teamBStats.FirstOrDefault()?.RoundsPlayed ?? 0;
                    
                    if (teamARounds > teamBRounds)
                        return match.TeamA;
                    else if (teamBRounds > teamARounds)
                        return match.TeamB;
                }
            }

            // Fallback: return null if we can't determine the winner
            return null;
        }





        // GET: /Tournaments/AddTeams/5
        public async Task<IActionResult> AddTeams(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams)
                .ThenInclude(tt => tt.Team)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();

            var vm = new TournamentTeamsViewModel
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.FullName,
                SelectedTeamIds = tournament.TournamentTeams.Select(tt => tt.TeamId).ToList(),
                AvailableTeams = await _context.Teams
                    .Select(t => new TeamSelectItem { Id = t.Id, FullName = t.FullName })
                    .ToListAsync()
            };

            return View(vm);
        }

        // POST: /Tournaments/AddTeams
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> AddTeams(TournamentTeamsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var allErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Count > 0)
                    .Select(kvp => new
                    {
                        Field = kvp.Key,
                        Errors = kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    });

                return Json(allErrors); // <-- temporarily return JSON so you can see errors in browser
            }

            // Remove old teams
            var existing = _context.TournamentTeams
                .Where(tt => tt.TournamentId == model.TournamentId);
            _context.TournamentTeams.RemoveRange(existing);

            // Add new selected teams
            var newEntries = model.SelectedTeamIds
                .Select((teamId, index) => new TournamentTeam
                {
                    TournamentId = model.TournamentId,
                    TeamId = teamId,
                    Seed = index + 1
                });

            await _context.TournamentTeams.AddRangeAsync(newEntries);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = model.TournamentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBracket(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams)
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return NotFound();
            if (tournament.Series.Any())
            {
                TempData["Error"] = "Bracket already exists.";
                return RedirectToAction("Details", new { id = tournamentId });
            }

            if (!tournament.TournamentTeams.Any())
            {
                TempData["Error"] = "No teams added to tournament. Please add teams first.";
                return RedirectToAction("Details", new { id = tournamentId });
            }

            var service = new BracketService(_context, mapper);

            // Get teams ordered by their seed (1, 2, 3, etc.)
            var teams = await _context.TournamentTeams
                .Where(tt => tt.TournamentId == tournamentId)
                .Include(tt => tt.Team)
                .OrderBy(tt => tt.Seed)
                .Select(tt => tt.Team)
                .ToListAsync();

            var series = service.GenerateBracket(tournamentId, teams);

            // First, save all series without NextSeriesId relationships
            foreach (var s in series)
            {
                s.NextSeriesId = null; // Clear NextSeriesId temporarily
            }

            await _context.Series.AddRangeAsync(series);
            await _context.SaveChangesAsync();

            // Now wire up the NextSeriesId relationships after series have IDs
            if (tournament.Elimination == EliminationType.Single)
            {
                service.WireUpSeriesProgression(series);
            }
            else
            {
                var doubleEliminationService = new DoubleEliminationBracketService(_context, mapper);
                var upperBracketSeries = series.Where(s => s.Bracket == BracketType.Upper).ToList();
                var lowerBracketSeries = series.Where(s => s.Bracket == BracketType.Lower).ToList();
                var grandFinal = series.Where(s => s.Bracket == BracketType.GrandFinal).FirstOrDefault();
                doubleEliminationService.WireUpDoubleEliminationProgression(upperBracketSeries, lowerBracketSeries, grandFinal);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{tournament.Elimination} bracket generated with {teams.Count} teams!";
            return RedirectToAction("Details", new { id = tournamentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> DeleteBracket(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return NotFound();

            if (!tournament.Series.Any())
            {
                TempData["Error"] = "No bracket exists to delete.";
                return RedirectToAction("Details", new { id = tournamentId });
            }

            _context.Series.RemoveRange(tournament.Series);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Bracket deleted successfully.";
            return RedirectToAction("Details", new { id = tournamentId });
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Conclude(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams).ThenInclude(tt => tt.Team)
                .Include(t => t.Placements)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();

            var mvpEVPService = new MVPEVPService(_context);
            var formulaConfig = new MVFormulaConfiguration();

            var vm = new TournamentConclusionViewModel
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.FullName,
                TotalTeams = tournament.TournamentTeams.Count,
                EliminationType = tournament.Elimination.ToString(),
                ChampionTrophyDescription = $"Champion of {tournament.FullName}",
                MVFormulaConfig = formulaConfig
            };

            // Load MVP and EVP candidates
            vm.MVPCandidates = await mvpEVPService.CalculateRankedMVPCandidatesAsync(id, formulaConfig);
            vm.EVPCandidates = await mvpEVPService.CalculateRankedEVPCandidatesAsync(id, formulaConfig);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Conclude(TournamentConclusionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var mvpevpService = new MVPEVPService(_context);
                model.MVPCandidates = await mvpevpService.GetMVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                model.EVPCandidates = await mvpevpService.GetEVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                return View(model);
            }

            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams).ThenInclude(tt => tt.Team)
                    .ThenInclude(t => t.Transfers).ThenInclude(tr => tr.Player)
                .Include(t => t.Placements)
                .FirstOrDefaultAsync(t => t.Id == model.TournamentId);

            if (tournament == null) return NotFound();

            var trophyService = new TrophyService(_context);
            var mvpEVPService = new MVPEVPService(_context);

            // Handle trophy image upload (only for Champion Trophy)
            string? trophyImagePath = null;
            if (model.TrophyImageFile != null && model.TrophyImageFile.Length > 0)
            {
                try
                {
                    trophyImagePath = await ImageOptimizer.SaveWebpAsync(
                        model.TrophyImageFile,
                        env.WebRootPath,
                        Path.Combine("uploads", "Tournaments", "Trophies"),
                        maxWidth: 1920,
                        maxHeight: 1080,
                        quality: 85);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving trophy image: {ex.Message}");
                    model.MVPCandidates = await mvpEVPService.CalculateRankedMVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                    model.EVPCandidates = await mvpEVPService.CalculateRankedEVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                    return View(model);
                }
            }

            // Award MVP trophy (uses default MVP image, not trophy image)
            if (model.SelectedMVPId.HasValue)
            {
                var mvpPlayer = await _context.Players.FindAsync(model.SelectedMVPId.Value);
                if (mvpPlayer != null)
                {
                    await trophyService.AwardPlayerTrophyAsync(
                        model.SelectedMVPId.Value,
                        "MVP",
                        $"MVP of {tournament.FullName}",
                        tournament.Id,
                        null); // MVP uses default image, not trophy image
                }
            }

            // Award EVP trophies
            if (model.SelectedEVPIds.Any())
            {
                // Validate EVP selection - check team limits
                var evpPlayers = await _context.Players
                    .Where(p => model.SelectedEVPIds.Contains(p.Id))
                    .Include(p => p.Transfers.Where(t => t.Status == PlayerTeamStatus.Active && 
                        t.StartDate <= tournament.EndDate && 
                        (t.EndDate == null || t.EndDate >= tournament.StartDate)))
                    .ToListAsync();

                // Get MVP player's team if MVP is selected
                int? mvpTeamId = null;
                if (model.SelectedMVPId.HasValue)
                {
                    var mvpPlayer = evpPlayers.FirstOrDefault(p => p.Id == model.SelectedMVPId.Value);
                    if (mvpPlayer == null)
                    {
                        mvpPlayer = await _context.Players
                            .Include(p => p.Transfers.Where(t => t.Status == PlayerTeamStatus.Active && 
                                t.StartDate <= tournament.EndDate && 
                                (t.EndDate == null || t.EndDate >= tournament.StartDate)))
                            .FirstOrDefaultAsync(p => p.Id == model.SelectedMVPId.Value);
                    }
                    if (mvpPlayer != null && mvpPlayer.Transfers.Any())
                    {
                        var transfer = mvpPlayer.Transfers.First();
                        if (transfer.TeamId.HasValue)
                        {
                            mvpTeamId = transfer.TeamId.Value;
                        }
                    }
                }

                // Count awards per team (only for players with teams)
                var teamCounts = new Dictionary<int, int>();
                foreach (var player in evpPlayers)
                {
                    if (player.Transfers.Any())
                    {
                        var transfer = player.Transfers.First();
                        if (transfer.TeamId.HasValue)
                        {
                            int teamId = transfer.TeamId.Value;
                            if (!teamCounts.ContainsKey(teamId))
                                teamCounts[teamId] = 0;
                            teamCounts[teamId] += 1;
                        }
                    }
                }

                // Add MVP to count if same team
                if (mvpTeamId.HasValue)
                {
                    int mvpTeam = mvpTeamId.Value;
                    if (!teamCounts.ContainsKey(mvpTeam))
                        teamCounts[mvpTeam] = 0;
                    teamCounts[mvpTeam] += 1;
                }

                // Check if any team has more than 3 awards (only validate teams that exist)
                if (teamCounts.Values.Any(count => count > 3))
                {
                    ModelState.AddModelError("", "One team cannot win more than 3 MVP+EVP awards.");
                    model.MVPCandidates = await mvpEVPService.CalculateRankedMVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                    model.EVPCandidates = await mvpEVPService.CalculateRankedEVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                    return View(model);
                }

                await trophyService.AwardMultiplePlayerTrophiesAsync(
                    model.SelectedEVPIds,
                    "EVP",
                    $"EVP of {tournament.FullName}",
                    tournament.Id,
                    null); // EVP uses default image, not trophy image
            }

            // Award points for placements
            var pointsMap = new Dictionary<int, int>
            {
                { 1, 500 }, // 1st place = 500 pts
                { 2, 325 },
                { 3, 200 },
                { 4, 125 }
                // expand as needed
            };

            foreach (var placement in tournament.Placements)
            {
                if (!pointsMap.TryGetValue(placement.Placement, out var points))
                    continue;

                placement.PointsAwarded = points;

                // Get roster at tournament start
                var roster = placement.Team.Transfers
                .Where(tr =>
                    tr.StartDate <= tournament.StartDate &&
                    (tr.EndDate == null || tr.EndDate >= tournament.StartDate) &&
                    tr.Status == PlayerTeamStatus.Active)
                .GroupBy(tr => tr.PlayerId)
                .Select(g => g.OrderByDescending(tr => tr.StartDate).First().Player)
                .ToList();

                // Decide the "core" (for simplicity: top 3 players by presence)
                var corePlayers = roster.Take(3).ToList();

                // Find or create Core
                var core = await _context.Cores
                    .Include(c => c.Players)
                    .FirstOrDefaultAsync(c =>
                        corePlayers.All(cp => c.Players.Select(p => p.PlayerId).Contains(cp.Id))
                        && c.Players.Count == corePlayers.Count);

                if (core == null)
                {
                    core = new Core { Name = string.Join("/", corePlayers.Select(p => p.Nickname)) };
                    _context.Cores.Add(core);
                    await _context.SaveChangesAsync();

                    foreach (var p in corePlayers)
                    {
                        _context.CorePlayers.Add(new CorePlayer { CoreId = core.Id, PlayerId = p.Id });
                    }
                }

                // Award points
                _context.CoreTournamentPoints.Add(new CoreTournamentPoints
                {
                    CoreId = core.Id,
                    TournamentId = tournament.Id,
                    Points = points
                });
            }

            await _context.SaveChangesAsync();

            // Generate placements from bracket results
            await GeneratePlacementsFromBracket(tournament);

            // Award champion trophy AFTER placements are generated
            if (model.AwardChampionTrophy)
            {
                // Reload tournament with fresh placements
                tournament = await _context.Tournaments
                    .Include(t => t.Placements)
                    .FirstOrDefaultAsync(t => t.Id == model.TournamentId);

                var champion = tournament.Placements
                    .Where(p => p.Placement == 1)
                    .FirstOrDefault();

                if (champion != null)
                {
                    // Use trophy image if provided, otherwise null (will use default)
                    string? championTrophyImagePath = null;
                    if (model.TrophyImageFile != null && model.TrophyImageFile.Length > 0)
                    {
                        try
                        {
                            championTrophyImagePath = await ImageOptimizer.SaveWebpAsync(
                                model.TrophyImageFile,
                                env.WebRootPath,
                                Path.Combine("uploads", "Tournaments", "Trophies"),
                                maxWidth: 1920,
                                maxHeight: 1080,
                                quality: 85);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Error saving champion trophy image: {ex.Message}");
                        }
                    }
                    
                    await trophyService.AwardChampionTrophyAsync(
                        tournament.Id, 
                        champion.TeamId, 
                        model.ChampionTrophyDescription,
                        championTrophyImagePath);
                    Console.WriteLine($"🏆 Awarded champion trophy to team {champion.TeamId} for tournament {tournament.Id}");
                }
                else
                {
                    Console.WriteLine($"⚠️ No champion found in placements for tournament {tournament.Id}");
                }
            }

            TempData["Success"] = $"Tournament '{tournament.FullName}' concluded with trophies and points awarded!";
            return RedirectToAction("Details", new { id = model.TournamentId });
        }

        private async Task GeneratePlacementsFromBracket(Tournament tournament)
        {
            // Clear existing placements
            _context.TournamentPlacements.RemoveRange(tournament.Placements);

            // Get all participating teams
            var participatingTeams = await _context.TournamentTeams
                .Include(tt => tt.Team)
                .Where(tt => tt.TournamentId == tournament.Id)
                .OrderBy(tt => tt.Seed)
                .Select(tt => tt.Team)
                .ToListAsync();

            // Get all series with match results
            var allSeries = await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.WinnerTeam)
                .Include(s => s.Matches)
                .Where(s => s.TournamentId == tournament.Id)
                .ToListAsync();

            var placements = new List<TournamentPlacement>();

            if (tournament.Elimination == EliminationType.Single)
            {
                await GenerateSingleEliminationPlacements(allSeries, participatingTeams, placements, tournament);
            }
            else
            {
                await GenerateDoubleEliminationPlacements(allSeries, participatingTeams, placements, tournament);
            }

            // Add placements to database
            foreach (var placement in placements)
            {
                _context.TournamentPlacements.Add(placement);
            }

            await _context.SaveChangesAsync();
        }

        private async Task GenerateSingleEliminationPlacements(List<Series> allSeries, List<Team> participatingTeams, List<TournamentPlacement> placements, Tournament tournament)
        {
            Console.WriteLine($"🎯 Starting single elimination placement generation for {participatingTeams.Count} teams");
            Console.WriteLine($"🎯 Participating teams: {string.Join(", ", participatingTeams.Select(t => $"{t.FullName} (ID: {t.Id})"))}");

            // Get all series ordered by round (highest first)
            var seriesByRound = allSeries
                .Where(s => s.Bracket == BracketType.Upper)
                .GroupBy(s => s.Round)
                .OrderByDescending(g => g.Key)
                .ToList();

            if (!seriesByRound.Any())
            {
                Console.WriteLine("❌ No series found for placement generation");
                return;
            }

            Console.WriteLine($"🎯 Series by round: {string.Join(", ", seriesByRound.Select(g => $"Round {g.Key}: {g.Count()} series"))}");

            // Track teams by elimination round
            var eliminatedTeams = new Dictionary<int, List<Team>>();
            var remainingTeams = new HashSet<Team>(participatingTeams);

            // Process rounds from highest to lowest to track eliminations
            foreach (var roundGroup in seriesByRound)
            {
                var roundEliminated = new List<Team>();
                
                Console.WriteLine($"🎯 Processing Round {roundGroup.Key} with {roundGroup.Count()} series");
                
                foreach (var series in roundGroup)
                {
                    Console.WriteLine($"🎯 Series {series.Id}: {series.TeamA?.FullName} vs {series.TeamB?.FullName}, Finished: {series.isFinished}, Winner: {series.WinnerTeam?.FullName}");
                    
                    if (series.isFinished && series.TeamA != null && series.TeamB != null && series.WinnerTeam != null)
                    {
                        var winner = series.WinnerTeam;
                        var loser = series.TeamA == winner ? series.TeamB : series.TeamA;
                        
                        if (loser != null && remainingTeams.Contains(loser))
                        {
                            roundEliminated.Add(loser);
                            remainingTeams.Remove(loser);
                            Console.WriteLine($"🎯 Team {loser.FullName} (ID: {loser.Id}) eliminated in Round {roundGroup.Key} by {winner.FullName}");
                        }
                    }
                }

                if (roundEliminated.Any())
                {
                    eliminatedTeams[roundGroup.Key] = roundEliminated;
                    Console.WriteLine($"🎯 Round {roundGroup.Key} eliminated: {string.Join(", ", roundEliminated.Select(t => $"{t.FullName} (ID: {t.Id})"))}");
                }
            }

            Console.WriteLine($"🎯 Remaining teams after elimination: {string.Join(", ", remainingTeams.Select(t => $"{t.FullName} (ID: {t.Id})"))}");

            // Create placements with proper shared positions
            int currentPlacement = 1;

            // 1st place - winner of final (remaining team)
            if (remainingTeams.Count == 1)
            {
                var winner = remainingTeams.First();
                placements.Add(CreatePlacement(tournament, winner, currentPlacement));
                Console.WriteLine($"🏆 1st place: {winner.FullName} (ID: {winner.Id})");
                currentPlacement++;
            }

            // Process eliminations from final round to first round
            foreach (var roundGroup in seriesByRound)
            {
                if (eliminatedTeams.ContainsKey(roundGroup.Key))
                {
                    var roundEliminated = eliminatedTeams[roundGroup.Key];
                    
                    // Calculate how many teams to advance from this round
                    int teamsAdvancing = roundGroup.Count();
                    int teamsEliminated = roundEliminated.Count;
                    
                    Console.WriteLine($"🎯 Round {roundGroup.Key}: {teamsEliminated} teams eliminated, {teamsAdvancing} teams advancing");
                    
                    // All teams eliminated in this round share the same placement
                    foreach (var team in roundEliminated)
                    {
                        placements.Add(CreatePlacement(tournament, team, currentPlacement));
                        Console.WriteLine($"Place {currentPlacement}: {team.FullName} (ID: {team.Id})");
                    }
                    currentPlacement += teamsEliminated;
                }
            }

            // Ensure ALL teams are placed (fallback for any missing teams)
            var placedTeamIds = placements.Select(p => p.TeamId).ToHashSet();
            var unplacedTeams = participatingTeams.Where(t => !placedTeamIds.Contains(t.Id)).ToList();
            
            if (unplacedTeams.Any())
            {
                Console.WriteLine($"⚠️ Found {unplacedTeams.Count} unplaced teams: {string.Join(", ", unplacedTeams.Select(t => $"{t.FullName} (ID: {t.Id})"))}");
                foreach (var team in unplacedTeams)
                {
                    placements.Add(CreatePlacement(tournament, team, currentPlacement));
                    Console.WriteLine($"Unplaced team {currentPlacement}: {team.FullName} (ID: {team.Id})");
                }
            }

            // Debug logging to verify all teams are placed
            Console.WriteLine($"🎯 Single Elimination Placement Generation Summary:");
            Console.WriteLine($"   Total participating teams: {participatingTeams.Count}");
            Console.WriteLine($"   Total placements created: {placements.Count}");
            Console.WriteLine($"   Placed team IDs: [{string.Join(", ", placements.Select(p => p.TeamId).OrderBy(id => id))}]");
            Console.WriteLine($"   Participating team IDs: [{string.Join(", ", participatingTeams.Select(t => t.Id).OrderBy(id => id))}]");
            
            // Group placements by placement number for debugging
            var placementGroups = placements.GroupBy(p => p.Placement).OrderBy(g => g.Key);
            foreach (var group in placementGroups)
            {
                Console.WriteLine($"   Placement {group.Key}: {group.Count()} teams - {string.Join(", ", group.Select(p => $"Team {p.TeamId}"))}");
            }
            
            if (placements.Count != participatingTeams.Count)
            {
                Console.WriteLine($"❌ WARNING: Single elimination placement count mismatch! Expected {participatingTeams.Count}, got {placements.Count}");
            }
        }

        private async Task GenerateDoubleEliminationPlacements(List<Series> allSeries, List<Team> participatingTeams, List<TournamentPlacement> placements, Tournament tournament)
        {
            Console.WriteLine($"🎯 Starting double elimination placement generation for {participatingTeams.Count} teams");

            // Track placed team IDs to prevent duplicates
            var placedTeamIds = new HashSet<int>();

            // Find grand final
            var grandFinal = allSeries
                .Where(s => s.Bracket == BracketType.GrandFinal)
                .FirstOrDefault();

            if (grandFinal?.isFinished == true && grandFinal.WinnerTeam != null)
            {
                // 1st place - winner of grand final
                if (!placedTeamIds.Contains(grandFinal.WinnerTeam.Id))
                {
                    placements.Add(CreatePlacement(tournament, grandFinal.WinnerTeam, 1));
                    placedTeamIds.Add(grandFinal.WinnerTeam.Id);
                    Console.WriteLine($"🏆 1st place: {grandFinal.WinnerTeam.FullName}");
                }

                // 2nd place - loser of grand final
                var runnerUp = grandFinal.TeamA == grandFinal.WinnerTeam ? grandFinal.TeamB : grandFinal.TeamA;
                if (runnerUp != null && !placedTeamIds.Contains(runnerUp.Id))
                {
                    placements.Add(CreatePlacement(tournament, runnerUp, 2));
                    placedTeamIds.Add(runnerUp.Id);
                    Console.WriteLine($"🥈 2nd place: {runnerUp.FullName}");
                }

                // 3rd place - loser of upper bracket final (the team that lost to grand final winner in upper bracket)
                var upperBracketFinal = allSeries
                    .Where(s => s.Bracket == BracketType.Upper)
                    .OrderByDescending(s => s.Round)
                    .FirstOrDefault();

                if (upperBracketFinal?.isFinished == true)
                {
                    var upperBracketLoser = upperBracketFinal.TeamA == grandFinal.WinnerTeam ? upperBracketFinal.TeamB : upperBracketFinal.TeamA;
                    if (upperBracketLoser != null && upperBracketLoser.Id != runnerUp?.Id && !placedTeamIds.Contains(upperBracketLoser.Id))
                    {
                        placements.Add(CreatePlacement(tournament, upperBracketLoser, 3));
                        placedTeamIds.Add(upperBracketLoser.Id);
                        Console.WriteLine($"🥉 3rd place: {upperBracketLoser.FullName}");
                    }
                }

                // Track eliminated teams by elimination round
                var eliminatedTeams = new Dictionary<int, List<Team>>();

                // Process upper bracket eliminations
                var upperBracketSeries = allSeries
                    .Where(s => s.Bracket == BracketType.Upper && s.isFinished)
                    .OrderByDescending(s => s.Round)
                    .ThenByDescending(s => s.Position)
                    .ToList();

                foreach (var series in upperBracketSeries)
                {
                    if (series.WinnerTeam != null)
                    {
                        var loser = series.TeamA == series.WinnerTeam ? series.TeamB : series.TeamA;
                        if (loser != null && !placedTeamIds.Contains(loser.Id))
                        {
                            // Teams eliminated in upper bracket final get 4th place
                            if (series.Round == upperBracketFinal.Round)
                            {
                                placements.Add(CreatePlacement(tournament, loser, 4));
                                placedTeamIds.Add(loser.Id);
                                Console.WriteLine($"4th place: {loser.FullName}");
                            }
                            else
                            {
                                // Other upper bracket eliminations
                                if (!eliminatedTeams.ContainsKey(series.Round))
                                    eliminatedTeams[series.Round] = new List<Team>();
                                if (!eliminatedTeams[series.Round].Any(t => t.Id == loser.Id))
                                    eliminatedTeams[series.Round].Add(loser);
                            }
                        }
                    }
                }

                // Process lower bracket eliminations
                var lowerBracketSeries = allSeries
                    .Where(s => s.Bracket == BracketType.Lower && s.isFinished)
                    .OrderByDescending(s => s.Round)
                    .ThenByDescending(s => s.Position)
                    .ToList();

                foreach (var series in lowerBracketSeries)
                {
                    if (series.WinnerTeam != null)
                    {
                        var loser = series.TeamA == series.WinnerTeam ? series.TeamB : series.TeamA;
                        if (loser != null && !placedTeamIds.Contains(loser.Id))
                        {
                            if (!eliminatedTeams.ContainsKey(series.Round + 100)) // Offset to separate from upper bracket
                                eliminatedTeams[series.Round + 100] = new List<Team>();
                            if (!eliminatedTeams[series.Round + 100].Any(t => t.Id == loser.Id))
                                eliminatedTeams[series.Round + 100].Add(loser);
                        }
                    }
                }

                // Assign placements based on elimination order
                int currentPlacement = 5;
                var sortedEliminations = eliminatedTeams.OrderByDescending(kvp => kvp.Key);
                
                foreach (var elimination in sortedEliminations)
                {
                    var teamsInThisElimination = elimination.Value.Where(t => !placedTeamIds.Contains(t.Id)).ToList();
                    foreach (var team in teamsInThisElimination)
                    {
                        placements.Add(CreatePlacement(tournament, team, currentPlacement));
                        placedTeamIds.Add(team.Id);
                        Console.WriteLine($"Place {currentPlacement}: {team.FullName}");
                    }
                    currentPlacement += teamsInThisElimination.Count;
                }
            }

            // Ensure ALL teams are placed (fallback for any missing teams)
            var unplacedTeams = participatingTeams.Where(t => !placedTeamIds.Contains(t.Id)).ToList();
            
            if (unplacedTeams.Any())
            {
                Console.WriteLine($"⚠️ Found {unplacedTeams.Count} unplaced teams: {string.Join(", ", unplacedTeams.Select(t => t.FullName))}");
                int lastPlacement = placements.Any() ? placements.Max(p => p.Placement) + 1 : 1;
                foreach (var team in unplacedTeams)
                {
                    if (!placedTeamIds.Contains(team.Id))
                    {
                        placements.Add(CreatePlacement(tournament, team, lastPlacement));
                        placedTeamIds.Add(team.Id);
                        Console.WriteLine($"Unplaced team {lastPlacement}: {team.FullName}");
                    }
                }
            }

            // Debug logging to verify all teams are placed
            Console.WriteLine($"🎯 Double Elimination Placement Generation Summary:");
            Console.WriteLine($"   Total participating teams: {participatingTeams.Count}");
            Console.WriteLine($"   Total placements created: {placements.Count}");
            Console.WriteLine($"   Placed team IDs: [{string.Join(", ", placements.Select(p => p.TeamId).OrderBy(id => id))}]");
            Console.WriteLine($"   Participating team IDs: [{string.Join(", ", participatingTeams.Select(t => t.Id).OrderBy(id => id))}]");
            
            // Check for duplicates
            var duplicateTeamIds = placements.GroupBy(p => p.TeamId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateTeamIds.Any())
            {
                Console.WriteLine($"❌ ERROR: Found duplicate team placements! Team IDs: [{string.Join(", ", duplicateTeamIds)}]");
            }
            
            if (placements.Count != participatingTeams.Count)
            {
                Console.WriteLine($"❌ WARNING: Double elimination placement count mismatch! Expected {participatingTeams.Count}, got {placements.Count}");
            }
        }

        private TournamentPlacement CreatePlacement(Tournament tournament, Team team, int placement)
        {
            return new TournamentPlacement
            {
                TournamentId = tournament.Id,
                TeamId = team.Id,
                Placement = placement,
                PointsAwarded = GetPointsForPlacement(tournament, placement),
                // Prize amount would be calculated here if needed
            };
        }

        private int GetPointsForPlacement(Tournament tournament, int placement)
        {
            // Try to parse the points configuration JSON
            if (!string.IsNullOrEmpty(tournament.PointsConfiguration))
            {
                try
                {
                    var pointsConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(tournament.PointsConfiguration);
                    if (pointsConfig != null && pointsConfig.ContainsKey(placement.ToString()))
                    {
                        return pointsConfig[placement.ToString()];
                    }
                }
                catch
                {
                    // Fall back to default if JSON parsing fails
                }
            }

            // Default point distribution if no configuration (scalable for any placement)
            return placement switch
            {
                1 => 500,
                2 => 325,
                3 => 200,
                4 => 125,
                5 => 100,
                6 => 75,
                7 => 50,
                8 => 25,
                9 => 20,
                10 => 15,
                11 => 12,
                12 => 10,
                13 => 8,
                14 => 6,
                15 => 4,
                16 => 2,
                _ => Math.Max(1, 20 - placement) // Dynamic calculation for higher placements
            };
        }

        private decimal GetPrizeForPlacement(Tournament tournament, int placement)
        {
            // Try to parse the prize configuration JSON
            if (!string.IsNullOrEmpty(tournament.PrizeConfiguration))
            {
                try
                {
                    var prizeConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, decimal>>(tournament.PrizeConfiguration);
                    if (prizeConfig != null && prizeConfig.ContainsKey(placement.ToString()))
                    {
                        return tournament.PrizePool * prizeConfig[placement.ToString()];
                    }
                }
                catch
                {
                    // Fall back to default if JSON parsing fails
                }
            }

            // Default prize distribution if no configuration (scalable for any placement)
            if (tournament.PrizePool > 0)
            {
                return placement switch
                {
                    1 => tournament.PrizePool * 0.50m, // 50%
                    2 => tournament.PrizePool * 0.30m, // 30%
                    3 => tournament.PrizePool * 0.20m, // 20%
                    4 => tournament.PrizePool * 0.10m, // 10%
                    5 => tournament.PrizePool * 0.05m, // 5%
                    6 => tournament.PrizePool * 0.03m, // 3%
                    7 => tournament.PrizePool * 0.02m, // 2%
                    8 => tournament.PrizePool * 0.01m, // 1%
                    _ => 0 // No prize for placements beyond 8th
                };
            }

            return 0;
        }

        private IEnumerable<TournamentOrganizersServiceModel> AllOrganizers()
        {
            return _context.Organizers
                .ProjectTo<TournamentOrganizersServiceModel>(this.con)
                .ToList();
        }
        private IEnumerable<TournamentGamesServiceModel> AllGames()
        {
            return _context.Games
                .ProjectTo<TournamentGamesServiceModel>(this.con)
                .ToList();
        }
        private bool IsByeMatch(Series series)
        {
            // A match is a bye if one team is null and the other is not
            // But we should still show TBD vs TBD matches
            return (series.TeamA == null && series.TeamB != null) || 
                   (series.TeamA != null && series.TeamB == null);
        }

        private IEnumerable<SelectListItem>? AllEliminationTypes()
        {
            return Enum.GetValues(typeof(EliminationType))
                .Cast<EliminationType>()
                .Select(e => new SelectListItem
                {
                    Value = e.ToString(),
                    Text = e.ToString()
                });
        }

        /// <summary>
        /// Test endpoint to verify bracket generation logic
        /// </summary>
        [HttpGet("Tournaments/TestBracketGeneration")]
        public IActionResult TestBracketGeneration()
        {
            try
            {
                var bracketService = new BracketService(_context, mapper);
                bracketService.TestBracketGeneration();
                
                return Json(new { 
                    success = true, 
                    message = "Bracket generation test completed. Check console output for details." 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Test failed: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// API endpoint for searching players (for manual MVP/EVP selection)
        /// </summary>
        [HttpGet("api/players/search")]
        public async Task<IActionResult> SearchPlayers([FromQuery] string query, [FromQuery] int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Ok(new List<object>());
            }

            var players = await _context.Players
                .Where(p => p.Nickname.Contains(query))
                .OrderBy(p => p.Nickname)
                .Take(limit)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Nickname
                })
                .ToListAsync();

            return Ok(players);
        }
    }
}
