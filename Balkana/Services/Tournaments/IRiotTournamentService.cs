using Balkana.Data.Models;

namespace Balkana.Services.Tournaments
{
    public interface IRiotTournamentService
    {
        /// <summary>
        /// Register a provider with Riot API (one-time setup)
        /// </summary>
        Task<int> RegisterProviderAsync(string region, string callbackUrl = "");

        /// <summary>
        /// Create a new tournament with Riot API
        /// </summary>
        Task<RiotTournament> CreateTournamentAsync(string name, int providerId, string region, int? internalTournamentId = null);

        /// <summary>
        /// Generate tournament codes for a specific tournament
        /// </summary>
        Task<List<RiotTournamentCode>> GenerateTournamentCodesAsync(
            int riotTournamentId,
            int count,
            int? seriesId = null,
            int? teamAId = null,
            int? teamBId = null,
            string description = null,
            string mapType = "SUMMONERS_RIFT",
            string pickType = "TOURNAMENT_DRAFT",
            string spectatorType = "ALL",
            int teamSize = 5,
            List<string> allowedSummonerIds = null,
            string metadata = null);

        /// <summary>
        /// Get details about a specific tournament code
        /// </summary>
        Task<RiotTournamentCode> GetTournamentCodeDetailsAsync(string code);

        /// <summary>
        /// Get match IDs for a specific tournament code
        /// </summary>
        Task<List<long>> GetMatchIdsByTournamentCodeAsync(string code);

        /// <summary>
        /// Update a tournament code to mark it as used and link to match
        /// </summary>
        Task UpdateTournamentCodeWithMatchAsync(string code, string matchId, int? matchDbId = null);

        /// <summary>
        /// Get all tournaments
        /// </summary>
        Task<List<RiotTournament>> GetAllTournamentsAsync();

        /// <summary>
        /// Get a specific tournament with its codes
        /// </summary>
        Task<RiotTournament> GetTournamentByIdAsync(int id);

        /// <summary>
        /// Get all codes for a tournament
        /// </summary>
        Task<List<RiotTournamentCode>> GetTournamentCodesAsync(int riotTournamentId);

        /// <summary>
        /// Get unused tournament codes
        /// </summary>
        Task<List<RiotTournamentCode>> GetUnusedTournamentCodesAsync(int? riotTournamentId = null);
    }
}

