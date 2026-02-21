namespace Balkana.Services.Riot
{
    public interface IDDragonVersionService
    {
        /// <summary>
        /// Resolves the Data Dragon CDN version for a game version (e.g. "14.2.123.456" -> "14.2.1").
        /// Falls back to latest if no match found.
        /// </summary>
        Task<string> GetDDragonVersionAsync(string? gameVersion);
    }
}
