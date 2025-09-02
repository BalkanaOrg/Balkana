using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Controllers
{
    public class SeriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Series
        public async Task<IActionResult> Index()
        {
            var series = _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Tournament.Game)
                .Include(s => s.Tournament);
            return View(await series.ToListAsync());
        }

        // GET: Series/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var series = await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Tournament.Game)
                .Include(s => s.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (series == null) return NotFound();

            return View(series);
        }

        // GET: Series/Create
        public IActionResult Create()
        {
            ViewData["TeamAId"] = new SelectList(_context.Teams, "Id", "FullName");
            ViewData["TeamBId"] = new SelectList(_context.Teams, "Id", "FullName");
            ViewData["GameId"] = new SelectList(_context.Games, "Id", "FullName");
            ViewData["TournamentId"] = new SelectList(_context.Tournaments, "Id", "FullName");
            return View();
        }

        // POST: Series/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,TeamAId,TeamBId,GameId,TournamentId,DatePlayed")] Series series)
        {
            if (ModelState.ErrorCount<=4)
            {
                _context.Add(series);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(series);
        }

        // GET: Series/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var series = await _context.Series
                .Include(s => s.Tournament)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null) return NotFound();

            ViewData["TeamAId"] = new SelectList(_context.Teams, "Id", "FullName", series.TeamAId);
            ViewData["TeamBId"] = new SelectList(_context.Teams, "Id", "FullName", series.TeamBId);
            ViewData["GameId"] = new SelectList(_context.Games, "Id", "FullName", series.Tournament.GameId);
            ViewData["TournamentId"] = new SelectList(_context.Tournaments, "Id", "FullName", series.TournamentId);

            return View(series);
        }

        // POST: Series/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,TeamAId,TeamBId,GameId,TournamentId,DatePlayed")] Series series)
        {
            if (id != series.Id) return NotFound();

            if (ModelState.ErrorCount <= 4)
            {
                _context.Update(series);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(series);
        }

        // GET: Series/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var series = await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Tournament.Game)
                .Include(s => s.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (series == null) return NotFound();

            return View(series);
        }

        // POST: Series/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var series = await _context.Series.FindAsync(id);
            if (series != null)
            {
                _context.Series.Remove(series);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
