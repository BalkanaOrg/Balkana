using Balkana.Data;
using Balkana.Data.DTOs;
using Balkana.Data.Models;

namespace Balkana.Services.Matches
{
    public interface IMatchImporter
    {
        Task<List<Match>> ImportMatchAsync(string externalMatchId, ApplicationDbContext db);

        Task<ICollection<ExternalMatchSummary>> GetMatchHistoryAsync(string profileId);
    }
}
