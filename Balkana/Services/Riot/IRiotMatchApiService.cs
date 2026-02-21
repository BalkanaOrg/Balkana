using Balkana.Data.DTOs.Riot;

namespace Balkana.Services.Riot
{
    public interface IRiotMatchApiService
    {
        /// <summary>
        /// Fetches match data from Riot Match-v5 API.
        /// matchId format: {platform}_{gameId} (e.g. EUW1_1234567890, EUNE1_1234567890)
        /// </summary>
        Task<RiotMatchDto?> GetMatchAsync(string matchId);
    }
}
