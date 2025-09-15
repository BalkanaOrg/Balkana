using Balkana.Data.DTOs.FaceIt;

namespace Balkana.Services.Admin
{
    public interface IExternalApiService
    {
        Task<List<FaceItPlayerDTO>> SearchFaceitPlayersAsync(string nickname);
        Task<string?> SearchRiotPlayerAsync(string gameName, string tagLine, string region);
    }
}
