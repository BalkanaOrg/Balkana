namespace Balkana.Services.Series
{
    using Balkana.Data.Models;

    public interface ISeriesService
    {
        Task<IEnumerable<Series>> GetAllSeriesAsync();
        Task<Series?> GetSeriesByIdAsync(int id);
        Task AddSeriesAsync(Series series);
        Task UpdateSeriesAsync(Series series);
        Task DeleteSeriesAsync(int id);
    }
}
