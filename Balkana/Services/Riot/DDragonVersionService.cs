using System.Text.Json;

namespace Balkana.Services.Riot
{
    public class DDragonVersionService : IDDragonVersionService
    {
        private readonly HttpClient _http;
        private static readonly object _cacheLock = new();
        private static List<string>? _versionsCache;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private const int CacheMinutes = 60;

        public DDragonVersionService(HttpClient http)
        {
            _http = http;
            _http.BaseAddress = new Uri("https://ddragon.leagueoflegends.com/");
        }

        public async Task<string> GetDDragonVersionAsync(string? gameVersion)
        {
            var versions = await GetVersionsAsync();
            if (versions == null || versions.Count == 0)
                return "14.2.1"; // sensible fallback

            if (string.IsNullOrWhiteSpace(gameVersion))
                return versions[0];

            // gameVersion: "14.2.123.456" or "13.24.567" -> we need "14.2.1" or "13.24.1"
            var parts = gameVersion.Trim().Split('.');
            if (parts.Length < 2)
                return versions[0];

            var major = parts[0];
            var minor = parts[1];
            var prefix = $"{major}.{minor}.";

            var match = versions.FirstOrDefault(v =>
                v.StartsWith(prefix, StringComparison.Ordinal) &&
                v.Length > prefix.Length &&
                char.IsDigit(v[prefix.Length]));

            return match ?? versions[0];
        }

        private async Task<List<string>?> GetVersionsAsync()
        {
            lock (_cacheLock)
            {
                if (_versionsCache != null && DateTime.UtcNow < _cacheExpiry)
                    return _versionsCache;
            }

            try
            {
                var json = await _http.GetStringAsync("api/versions.json");
                var versions = JsonSerializer.Deserialize<List<string>>(json);
                var filtered = versions?
                    .Where(v => !v.StartsWith("lolpatch_", StringComparison.Ordinal) && !v.StartsWith("0.", StringComparison.Ordinal))
                    .ToList() ?? new List<string>();

                lock (_cacheLock)
                {
                    _versionsCache = filtered;
                    _cacheExpiry = DateTime.UtcNow.AddMinutes(CacheMinutes);
                }
                return _versionsCache;
            }
            catch
            {
                return _versionsCache ?? new List<string> { "14.2.1" };
            }
        }
    }
}
