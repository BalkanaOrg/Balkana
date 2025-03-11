using Balkana.Data.Models;

namespace Balkana.Data.Repositories
{
    public interface ISeriesRepository
    {
        Task<IEnumerable<Series>> GetAllSeriesAsync();
        Task<Series?> GetSeriesByIdAsync(int id);
        Task AddSeriesAsync(Series series);
        Task UpdateSeriesAsync(Series series);
        Task DeleteSeriesAsync(int id);
    }
}