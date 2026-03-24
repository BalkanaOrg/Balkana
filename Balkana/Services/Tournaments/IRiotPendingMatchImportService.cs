namespace Balkana.Services.Tournaments
{
    public interface IRiotPendingMatchImportService
    {
        /// <summary>
        /// Import a pending Riot match into the given series.
        /// Updates RiotTournamentCode if linked, and advances bracket.
        /// </summary>
        Task<(bool Success, string? Error)> ImportAsync(int pendingMatchId, int seriesId);
    }
}
