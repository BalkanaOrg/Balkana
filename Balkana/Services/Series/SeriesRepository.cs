using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Data.Repositories
{
    public class SeriesRepository : ISeriesRepository
    {
        private readonly ApplicationDbContext _context;

        public SeriesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Series>> GetAllSeriesAsync()
        {
            return await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Tournament.Game)
                .Include(s => s.Tournament)
                .Include(s => s.Matches)
                .ToListAsync();
        }

        public async Task<Series?> GetSeriesByIdAsync(int id)
        {
            return await _context.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Tournament.Game)
                .Include(s => s.Tournament)
                .Include(s => s.Matches)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddSeriesAsync(Series series)
        {
            _context.Series.Add(series);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSeriesAsync(Series series)
        {
            _context.Series.Update(series);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSeriesAsync(int id)
        {
            var series = await _context.Series.FindAsync(id);
            if (series != null)
            {
                _context.Series.Remove(series);
                await _context.SaveChangesAsync();
            }
        }
    }
}
