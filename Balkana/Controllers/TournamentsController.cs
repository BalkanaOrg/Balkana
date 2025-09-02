using Balkana.Data.Models;
using Balkana.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Balkana.Models.Tournaments;

namespace Balkana.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TournamentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tournament/Index
        public async Task<IActionResult> Index()
        {
            var tournaments = await _context.Tournaments
                .Include(t => t.Organizer)
                .Include(t => t.Game)
                .ToListAsync();

            return View(tournaments);
        }

        // GET: Tournament/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Organizer)
                .Include(t => t.Game)
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
            {
                return NotFound();
            }

            return View(tournament);
        }

        // GET: Tournament/Add
        public IActionResult Add()
        {
            return View(new TournamentFormViewModel());
        }

        // POST: Tournament/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(TournamentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var tournament = new Tournament
            {
                FullName = model.FullName,
                ShortName = model.ShortName,
                OrganizerId = model.OrganizerId,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                GameId = model.GameId
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
