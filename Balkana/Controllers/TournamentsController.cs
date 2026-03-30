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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        private bool CanManageTournaments() =>
            User.Identity?.IsAuthenticated == true
            && (User.IsInRole("Administrator") || User.IsInRole("Moderator"));

        // GET: Tournament/Index
        public async Task<IActionResult> Index()
        {
            var query = _context.Tournaments
                .Include(t => t.Organizer)
                .Include(t => t.Game)
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                .AsQueryable();

            if (!CanManageTournaments())
                query = query.Where(t => t.IsPublic);

            var tournaments = await query
                .OrderByDescending(c => c.StartDate)
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

            if (!tournament.IsPublic && !CanManageTournaments())
                return RedirectToAction(nameof(Index));

            ViewData["CanManageSeriesForfeit"] = CanManageTournaments();

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
            ViewData["TeamRostersById"] = participatingTeams
                .GroupBy(r => r.Team.Id)
                .ToDictionary(g => g.Key, g => g.SelectMany(r => r.Players).Distinct().ToList());

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
                model.Games = AllGames();
                model.Organizers = AllOrganizers();
                model.EliminationTypes = AllEliminationTypes();
                return View(model);
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
                    model.Games = AllGames();
                    model.Organizers = AllOrganizers();
                    model.EliminationTypes = AllEliminationTypes();
                    return View(model);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ General Exception while saving file: " + ex);
                    ModelState.AddModelError("", "Unexpected error while saving file.");
                    model.Games = AllGames();
                    model.Organizers = AllOrganizers();
                    model.EliminationTypes = AllEliminationTypes();
                    return View(model);
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
                PrizeConfiguration = model.PrizeConfiguration,
                IsPublic = model.IsPublic
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
                SelectedTeamIds = tournament.TournamentTeams.Select(tt => tt.TeamId).ToList(),
                IsPublic = tournament.IsPublic
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
            tournament.IsPublic = model.IsPublic;

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
                var imageBytes = await CaptureBracketFromTournamentPage(tournament);

                Response.Headers["Content-Type"] = "image/png";
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                Response.Headers["Content-Disposition"] = "inline";

                return File(imageBytes, "image/png", $"{tournament.FullName.Replace(" ", "_")}_bracket.png");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetBracketImage capture failed for tournament {id}: {ex}");
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

        private string BuildBracketStandaloneCapturePage(object bracketData)
        {
            var json = JsonSerializer.Serialize(
                bracketData,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <link rel=""stylesheet"" href=""https://unpkg.com/brackets-viewer/dist/brackets-viewer.min.css"" />
    <style>html,body{{margin:0;background:#0a0a0a;}}</style>
</head>
<body>
    <div id=""bracket-container"" class=""brackets-viewer"" style=""width:100%;min-height:500px;""></div>
    <script src=""https://unpkg.com/brackets-viewer/dist/brackets-viewer.min.js""></script>
    <script>
        (async function () {{
            const bracketData = {json};
            await bracketsViewer.render(
                {{
                    stages: bracketData.stages,
                    matches: bracketData.matches,
                    matchGames: bracketData.matchGames || [],
                    participants: bracketData.participants
                }},
                {{
                    selector: '#bracket-container',
                    orientation: 'horizontal',
                    participantOriginPlacement: 'before',
                    separatedByChildCountLabel: true,
                    highlightParticipantOnHover: true,
                    showSlotsOrigin: true,
                    showLowerBracketSlotsOrigin: true,
                    showPopoverOnMatchLabelClick: false,
                    showPopoverOnMatchClick: false,
                    highlightMatchOnHover: false
                }}
            );
        }})();
    </script>
</body>
</html>";
        }

        private async Task<byte[]> CaptureBracketFromTournamentPage(Tournament tournament)
        {
            const int minPngBytes = 5000;
            await new BrowserFetcher().DownloadAsync();

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            var viewPort = new ViewPortOptions
            {
                Width = 1920,
                Height = 1080,
                DeviceScaleFactor = 2
            };

            static bool ScreenshotOk(byte[]? bytes, int min) =>
                bytes != null && bytes.Length >= min;

            byte[]? fromDetails = null;
            try
            {
                using var page = await browser.NewPageAsync();
                await page.SetViewportAsync(viewPort);
                var tournamentUrl = $"{Request.Scheme}://{Request.Host}/Tournaments/Details/{tournament.Id}";
                await page.GoToAsync(tournamentUrl, WaitUntilNavigation.Networkidle2);

                try
                {
                    await page.WaitForSelectorAsync("#bracket-container .match", new WaitForSelectorOptions { Timeout = 20000 });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CaptureBracketFromTournamentPage: waiting for matches on details: {ex.Message}");
                }

                await Task.Delay(4000);

                var bracketElement = await page.QuerySelectorAsync("#bracket-container");
                if (bracketElement != null)
                    fromDetails = await bracketElement.ScreenshotDataAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CaptureBracketFromTournamentPage (details page): {ex}");
            }

            if (ScreenshotOk(fromDetails, minPngBytes))
                return fromDetails!;

            var bracketData = await GetBracketDataForImage(tournament);
            var html = BuildBracketStandaloneCapturePage(bracketData);

            using var standalonePage = await browser.NewPageAsync();
            await standalonePage.SetViewportAsync(viewPort);
            await standalonePage.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });
            await standalonePage.WaitForSelectorAsync("#bracket-container .match", new WaitForSelectorOptions { Timeout = 25000 });
            await Task.Delay(2000);

            var standaloneEl = await standalonePage.QuerySelectorAsync("#bracket-container");
            if (standaloneEl == null)
                throw new InvalidOperationException("Standalone bracket container not found.");

            var fromStandalone = await standaloneEl.ScreenshotDataAsync();
            if (!ScreenshotOk(fromStandalone, minPngBytes))
                throw new InvalidOperationException(
                    $"Bracket PNG too small (details: {fromDetails?.Length ?? 0}, standalone: {fromStandalone?.Length ?? 0} bytes).");

            return fromStandalone;
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
        [Authorize(Roles = "Administrator,Moderator")]
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

            var placementPointsService = new TournamentPlacementPointsService(_context);
            vm.TeamPointsPreview = await placementPointsService.BuildPointsPreviewAsync(id);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Conclude(TournamentConclusionViewModel model)
        {
            var mvpEVPService = new MVPEVPService(_context);

            if (!ModelState.IsValid)
            {
                model.MVPCandidates = await mvpEVPService.CalculateRankedMVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                model.EVPCandidates = await mvpEVPService.CalculateRankedEVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                await PopulateConcludePointsPreviewAsync(model);
                return View(model);
            }

            var tournament = await _context.Tournaments
                .Include(t => t.TournamentTeams).ThenInclude(tt => tt.Team)
                    .ThenInclude(t => t.Transfers).ThenInclude(tr => tr.Player)
                .Include(t => t.Placements)
                .FirstOrDefaultAsync(t => t.Id == model.TournamentId);

            if (tournament == null) return NotFound();

            if (model.MVPSourceType == MVPSourceType.Formula && model.SelectedMVPId.HasValue)
            {
                var mvpPool = await mvpEVPService.CalculateRankedMVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                if (!mvpPool.Any(c => c.PlayerId == model.SelectedMVPId.Value))
                {
                    ModelState.AddModelError(nameof(model.SelectedMVPId), "Selected MVP is not in the formula candidate pool for this tournament.");
                }
            }

            if (model.EVPSourceType == EVPSourceType.Formula && model.SelectedEVPIds is { Count: > 0 })
            {
                var evpPool = await mvpEVPService.CalculateRankedEVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                var allowed = evpPool.Select(c => c.PlayerId).ToHashSet();
                if (model.SelectedEVPIds.Any(id => !allowed.Contains(id)))
                {
                    ModelState.AddModelError(nameof(model.SelectedEVPIds), "One or more selected EVPs are not in the formula candidate pool for this tournament.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.MVPCandidates = await mvpEVPService.CalculateRankedMVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                model.EVPCandidates = await mvpEVPService.CalculateRankedEVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                await PopulateConcludePointsPreviewAsync(model);
                return View(model);
            }

            var trophyService = new TrophyService(_context);

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
                    await PopulateConcludePointsPreviewAsync(model);
                    return View(model);
                }
            }

            if (model.SelectedEVPIds.Any())
            {
                var evpPlayers = await _context.Players
                    .Where(p => model.SelectedEVPIds.Contains(p.Id))
                    .Include(p => p.Transfers.Where(t => t.Status == PlayerTeamStatus.Active &&
                        t.StartDate <= tournament.EndDate &&
                        (t.EndDate == null || t.EndDate >= tournament.StartDate)))
                    .ToListAsync();

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
                            mvpTeamId = transfer.TeamId.Value;
                    }
                }

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

                if (mvpTeamId.HasValue)
                {
                    int mvpTeam = mvpTeamId.Value;
                    if (!teamCounts.ContainsKey(mvpTeam))
                        teamCounts[mvpTeam] = 0;
                    teamCounts[mvpTeam] += 1;
                }

                if (teamCounts.Values.Any(count => count > 3))
                {
                    ModelState.AddModelError("", "One team cannot win more than 3 MVP+EVP awards.");
                    model.MVPCandidates = await mvpEVPService.CalculateRankedMVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                    model.EVPCandidates = await mvpEVPService.CalculateRankedEVPCandidatesAsync(model.TournamentId, model.MVFormulaConfig);
                    await PopulateConcludePointsPreviewAsync(model);
                    return View(model);
                }
            }

            var bracketPlacementService = new TournamentBracketPlacementService(_context);
            await bracketPlacementService.PersistPlacementsAsync(tournament.Id);

            var placementPointsService = new TournamentPlacementPointsService(_context);
            await placementPointsService.DistributeTournamentPlacementPointsAsync(tournament.Id);

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
                        null);
                }
            }

            if (model.SelectedEVPIds.Any())
            {
                await trophyService.AwardMultiplePlayerTrophiesAsync(
                    model.SelectedEVPIds,
                    "EVP",
                    $"EVP of {tournament.FullName}",
                    tournament.Id,
                    null);
            }

            if (model.AwardChampionTrophy)
            {
                var tournamentForChampion = await _context.Tournaments
                    .Include(t => t.Placements)
                    .FirstOrDefaultAsync(t => t.Id == model.TournamentId);

                if (tournamentForChampion != null)
                {
                    var champion = tournamentForChampion.Placements
                        .Where(p => p.Placement == 1)
                        .FirstOrDefault();

                    if (champion != null)
                    {
                        await trophyService.AwardChampionTrophyAsync(
                            tournamentForChampion.Id,
                            champion.TeamId,
                            model.ChampionTrophyDescription,
                            trophyImagePath);
                        Console.WriteLine($"🏆 Awarded champion trophy to team {champion.TeamId} for tournament {tournamentForChampion.Id}");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ No champion found in placements for tournament {tournamentForChampion.Id}");
                    }
                }
            }

            TempData["Success"] = $"Tournament '{model.TournamentName}' concluded with trophies and points awarded!";
            return RedirectToAction("Details", new { id = model.TournamentId });
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

        private async Task PopulateConcludePointsPreviewAsync(TournamentConclusionViewModel model)
        {
            var svc = new TournamentPlacementPointsService(_context);
            model.TeamPointsPreview = await svc.BuildPointsPreviewAsync(model.TournamentId);
        }
    }
}
