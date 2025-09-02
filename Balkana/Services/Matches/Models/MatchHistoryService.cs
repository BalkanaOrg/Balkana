using Balkana.Data;
using Balkana.Data.DTOs;
using Balkana.Data.Models;
using Balkana.Services.Matches;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Matches.Models
{
    public class MatchHistoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly Dictionary<string, IMatchImporter> _importers;

        public MatchHistoryService(ApplicationDbContext db, Dictionary<string, IMatchImporter> importers)
        {
            _db = db;
            _importers = importers;
        }

        public async Task<Match> ImportIfNotExistsAsync(string source, string matchId)
        {
            if (_db.Matches.Any(m => m.ExternalMatchId == matchId && m.Source == source))
                return null;

            if (!_importers.TryGetValue(source, out var importer))
                throw new ArgumentException($"No importer for source {source}");

            var match = await importer.ImportMatchAsync(matchId, _db);
            if (match != null)
            {
                _db.Matches.Add(match);
                await _db.SaveChangesAsync();
            }
            return match;
        }

        public async Task<ICollection<ExternalMatchSummary>> GetHistoryAsync(string source, string profileId)
        {
            if (!_importers.TryGetValue(source, out var importer))
                throw new ArgumentException($"No importer for source {source}");

            var history = await importer.GetMatchHistoryAsync(profileId);

            // Mark which ones are already in DB
            foreach (var match in history)
            {
                match.ExistsInDb = _db.Matches.Any(m => m.ExternalMatchId == match.ExternalMatchId && m.Source == match.Source);
            }

            return history;
        }
    }
}
