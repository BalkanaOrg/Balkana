using Balkana.Data;
using Balkana.Data.DTOs.Riot;
using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Balkana.Services.Tournaments
{
    public class RiotTournamentService : IRiotTournamentService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public RiotTournamentService(HttpClient httpClient, ApplicationDbContext context, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;
            _configuration = configuration;
            _apiKey = _configuration["Riot:ApiKey"];

            // Configure HttpClient for Tournament API
            // Use regional routing based on configuration (default: europe for EUW/EUNE)
            var tournamentRegion = _configuration["Riot:TournamentRegion"] ?? "europe";
            _httpClient.BaseAddress = new Uri($"https://{tournamentRegion}.api.riotgames.com/lol/tournament/v5/");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Riot-Token", _apiKey);
        }

        public async Task<int> RegisterProviderAsync(string region, string callbackUrl = "")
        {
            var request = new RiotProviderRegistrationDto
            {
                region = region.ToUpper(),
                url = string.IsNullOrEmpty(callbackUrl) ? "" : callbackUrl
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            
            // Log the request for debugging
            Console.WriteLine($"[RIOT API] POST {_httpClient.BaseAddress}providers");
            Console.WriteLine($"[RIOT API] Region: {region.ToUpper()}, Callback: {callbackUrl}");
            Console.WriteLine($"[RIOT API] API Key: {_apiKey.Substring(0, Math.Min(10, _apiKey.Length))}...");
            
            var response = await _httpClient.PostAsync("providers", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[RIOT API ERROR] Status: {response.StatusCode}");
                Console.WriteLine($"[RIOT API ERROR] Response: {error}");
                throw new Exception($"Failed to register provider: {response.StatusCode} - {error}");
            }

            var providerId = await response.Content.ReadFromJsonAsync<int>();
            Console.WriteLine($"[RIOT API] Successfully registered provider: {providerId}");
            return providerId;
        }

        public async Task<RiotTournament> CreateTournamentAsync(string name, int providerId, string region, int? internalTournamentId = null)
        {
            var request = new RiotTournamentRegistrationDto
            {
                providerId = providerId,
                name = name
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("tournaments", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create tournament: {response.StatusCode} - {error}");
            }

            var tournamentId = await response.Content.ReadFromJsonAsync<int>();

            var riotTournament = new RiotTournament
            {
                RiotTournamentId = tournamentId,
                Name = name,
                ProviderId = providerId,
                Region = region.ToUpper(),
                TournamentId = internalTournamentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(riotTournament);
            await _context.SaveChangesAsync();

            return riotTournament;
        }

        public async Task<List<RiotTournamentCode>> GenerateTournamentCodesAsync(
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
            string metadata = null)
        {
            var tournament = await _context.Set<RiotTournament>()
                .FirstOrDefaultAsync(t => t.RiotTournamentId == riotTournamentId);

            if (tournament == null)
                throw new Exception($"Riot Tournament {riotTournamentId} not found in database");

            var request = new RiotTournamentCodeRequestDto
            {
                allowedSummonerIds = allowedSummonerIds ?? new List<string>(),
                metadata = metadata ?? "",
                teamSize = teamSize,
                pickType = pickType,
                mapType = mapType,
                spectatorType = spectatorType
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"codes?tournamentId={riotTournamentId}&count={count}", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to generate tournament codes: {response.StatusCode} - {error}");
            }

            var codes = await response.Content.ReadFromJsonAsync<List<string>>();

            var tournamentCodes = new List<RiotTournamentCode>();
            foreach (var code in codes)
            {
                var tournamentCode = new RiotTournamentCode
                {
                    Code = code,
                    RiotTournamentId = tournament.Id,
                    SeriesId = seriesId,
                    TeamAId = teamAId,
                    TeamBId = teamBId,
                    Description = description,
                    MapType = mapType,
                    PickType = pickType,
                    SpectatorType = spectatorType,
                    TeamSize = teamSize,
                    AllowedSummonerIds = allowedSummonerIds != null ? string.Join(",", allowedSummonerIds) : null,
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                _context.Add(tournamentCode);
                tournamentCodes.Add(tournamentCode);
            }

            await _context.SaveChangesAsync();

            return tournamentCodes;
        }

        public async Task<RiotTournamentCode> GetTournamentCodeDetailsAsync(string code)
        {
            var response = await _httpClient.GetAsync($"codes/{code}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get tournament code details: {response.StatusCode} - {error}");
            }

            var details = await response.Content.ReadFromJsonAsync<RiotTournamentCodeDetailsDto>();

            var tournamentCode = await _context.Set<RiotTournamentCode>()
                .Include(tc => tc.RiotTournament)
                .Include(tc => tc.Series)
                .Include(tc => tc.TeamA)
                .Include(tc => tc.TeamB)
                .FirstOrDefaultAsync(tc => tc.Code == code);

            if (tournamentCode == null)
            {
                throw new Exception($"Tournament code {code} not found in database");
            }

            return tournamentCode;
        }

        public async Task<List<long>> GetMatchIdsByTournamentCodeAsync(string code)
        {
            var response = await _httpClient.GetAsync($"matches/by-code/{code}/ids");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get match IDs: {response.StatusCode} - {error}");
            }

            var matchIds = await response.Content.ReadFromJsonAsync<List<long>>();
            return matchIds ?? new List<long>();
        }

        public async Task UpdateTournamentCodeWithMatchAsync(string code, string matchId, int? matchDbId = null)
        {
            var tournamentCode = await _context.Set<RiotTournamentCode>()
                .FirstOrDefaultAsync(tc => tc.Code == code);

            if (tournamentCode == null)
                throw new Exception($"Tournament code {code} not found in database");

            tournamentCode.IsUsed = true;
            tournamentCode.MatchId = matchId;
            tournamentCode.MatchDbId = matchDbId;

            await _context.SaveChangesAsync();
        }

        public async Task<List<RiotTournament>> GetAllTournamentsAsync()
        {
            return await _context.Set<RiotTournament>()
                .Include(t => t.Tournament)
                .Include(t => t.TournamentCodes)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<RiotTournament> GetTournamentByIdAsync(int id)
        {
            return await _context.Set<RiotTournament>()
                .Include(t => t.Tournament)
                .Include(t => t.TournamentCodes)
                    .ThenInclude(tc => tc.Series)
                .Include(t => t.TournamentCodes)
                    .ThenInclude(tc => tc.TeamA)
                .Include(t => t.TournamentCodes)
                    .ThenInclude(tc => tc.TeamB)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<RiotTournamentCode>> GetTournamentCodesAsync(int riotTournamentId)
        {
            return await _context.Set<RiotTournamentCode>()
                .Include(tc => tc.Series)
                .Include(tc => tc.TeamA)
                .Include(tc => tc.TeamB)
                .Include(tc => tc.Match)
                .Where(tc => tc.RiotTournamentId == riotTournamentId)
                .OrderByDescending(tc => tc.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<RiotTournamentCode>> GetUnusedTournamentCodesAsync(int? riotTournamentId = null)
        {
            var query = _context.Set<RiotTournamentCode>()
                .Include(tc => tc.RiotTournament)
                .Include(tc => tc.Series)
                .Include(tc => tc.TeamA)
                .Include(tc => tc.TeamB)
                .Where(tc => !tc.IsUsed);

            if (riotTournamentId.HasValue)
            {
                query = query.Where(tc => tc.RiotTournamentId == riotTournamentId.Value);
            }

            return await query.OrderBy(tc => tc.CreatedAt).ToListAsync();
        }
    }
}

