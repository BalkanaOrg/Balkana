using Balkana.Data.DTOs;
using Balkana.Data;
using Balkana.Services.Matches;
using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;

public class MatchHistoryService
{
    private readonly ApplicationDbContext _db;
    private readonly Dictionary<string, IMatchImporter> _importers;

    public MatchHistoryService(ApplicationDbContext db, Dictionary<string, IMatchImporter> importers)
    {
        _db = db;
        _importers = importers;
    }

    public async Task<List<Match>> ImportIfNotExistsAsync(string source, string matchId)
    {
        // If *any* map from this match already exists, we skip
        if (_db.Matches.Any(m => m.ExternalMatchId == matchId && m.Source == source))
            return null;

        if (!_importers.TryGetValue(source, out var importer))
            throw new ArgumentException($"No importer for source {source}");

        var matches = await importer.ImportMatchAsync(matchId, _db);
        if (matches != null && matches.Count > 0)
        {
            _db.Matches.AddRange(matches);
            await _db.SaveChangesAsync();
        }

        return matches;
    }

    public async Task<ICollection<ExternalMatchSummary>> GetHistoryAsync(string source, string profileId)
    {
        if (!_importers.TryGetValue(source, out var importer))
            throw new ArgumentException($"No importer for source {source}");

        var history = await importer.GetMatchHistoryAsync(profileId);

        // Mark which ones are already in DB
        foreach (var match in history)
        {
            var externalId = match.ExternalMatchId; // the FACEIT series id

            match.ExistsInDb = await _db.Matches
                .AnyAsync(m => m.Source == match.Source &&
                               m.ExternalMatchId.StartsWith(externalId));
        }

        return history;
    }
}