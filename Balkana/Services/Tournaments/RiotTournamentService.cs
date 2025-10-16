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

            // Configure HttpClient for Tournament-stub API (testing environment)
            // Use regional routing based on configuration (default: europe for EUW/EUNE)
            var tournamentRegion = _configuration["Riot:TournamentRegion"] ?? "europe";
            _httpClient.BaseAddress = new Uri($"https://{tournamentRegion}.api.riotgames.com/lol/tournament-stub/v5/");
            _httpClient.DefaultRequestHeaders.Clear();
            
            // Tournament-stub API uses api_key parameter instead of X-Riot-Token header
            // We'll add the API key to each request URL instead of headers
            
            // Headers that match the working request from Riot's website
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("Origin", "https://developer.riotgames.com");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Configured Base URL: {_httpClient.BaseAddress}");
            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Configured Headers: {string.Join(", ", _httpClient.DefaultRequestHeaders.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] API Key Length: {_apiKey?.Length ?? 0}");
            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] API Key Format: {(_apiKey?.StartsWith("RGAPI-") == true ? "Valid RGAPI format" : "Invalid format")}");
        }

        public async Task<bool> TestApiKeyAsync()
        {
            try
            {
                // First, let's try a simple test with a different regional endpoint
                // Tournament-stub might require specific regional endpoints like euw1, na1, etc.
                var testRegions = new[] { "euw1", "eune1", "na1", "kr", "br1" };
                
                foreach (var testRegion in testRegions)
                {
                    try
                    {
                        var testUrl = $"https://{testRegion}.api.riotgames.com/lol/tournament-stub/v5/providers?api_key={_apiKey}";
                        Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Testing region: {testRegion}");
                        
                        var testRequest = new HttpRequestMessage(HttpMethod.Get, testUrl);
                        testRequest.Headers.Add("Accept", "application/json");
                        
                        var testResponse = await _httpClient.SendAsync(testRequest);
                        Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Region {testRegion} - Status: {testResponse.StatusCode}");
                        
                        if (testResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Success with region: {testRegion}");
                            return true;
                        }
                        else
                        {
                            var errorContent = await testResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Region {testRegion} error: {errorContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Region {testRegion} exception: {ex.Message}");
                    }
                }
                
                // If all regions fail, try the original approach
                var urlWithApiKey = $"providers?api_key={_apiKey}";
                var response = await _httpClient.GetAsync(urlWithApiKey);
                Console.WriteLine($"[RIOT TOURNAMENT-STUB API] API Key Test (GET providers) - Status: {response.StatusCode}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"[RIOT TOURNAMENT-STUB API] API Key is valid - providers endpoint accessible");
                    return true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine($"[RIOT TOURNAMENT-STUB API] API Key lacks permissions for Tournament-stub API");
                    
                    // Get the actual error response
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Error Response: {errorContent}");
                    
                    return false;
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RIOT TOURNAMENT-STUB API] API Key Test Error: {ex.Message}");
                return false;
            }
        }

        public async Task<int> RegisterProviderAsync(string region, string callbackUrl = "")
        {
            // Tournament-stub API might expect different region format
            // Try without the "1" suffix first (EUNE1 -> EUNE)
            var regionFormatted = region.ToUpper();
            if (regionFormatted.EndsWith("1"))
            {
                regionFormatted = regionFormatted.Substring(0, regionFormatted.Length - 1);
            }

            var request = new RiotProviderRegistrationDto
            {
                region = regionFormatted,
                url = string.IsNullOrEmpty(callbackUrl) ? "" : callbackUrl  // Empty for testing
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            
            // Add API key as URL parameter instead of header
            var urlWithApiKey = $"providers?api_key={_apiKey}";
            
            // Log the request for debugging
            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] POST {_httpClient.BaseAddress}{urlWithApiKey}");
            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Original Region: {region.ToUpper()}, Formatted: {regionFormatted}");
            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Callback: {callbackUrl}");
            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] API Key: {_apiKey.Substring(0, Math.Min(10, _apiKey.Length))}...");
            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Request Body: {JsonSerializer.Serialize(request)}");
            
            var response = await _httpClient.PostAsync(urlWithApiKey, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[RIOT TOURNAMENT-STUB API ERROR] Status: {response.StatusCode}");
                Console.WriteLine($"[RIOT TOURNAMENT-STUB API ERROR] Response: {error}");
                throw new Exception($"Failed to register provider: {response.StatusCode} - {error}");
            }

            var providerId = await response.Content.ReadFromJsonAsync<int>();
            Console.WriteLine($"[RIOT TOURNAMENT-STUB API] Successfully registered provider: {providerId}");
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
            var urlWithApiKey = $"tournaments?api_key={_apiKey}";
            var response = await _httpClient.PostAsync(urlWithApiKey, content);

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
            var urlWithApiKey = $"codes?tournamentId={riotTournamentId}&count={count}&api_key={_apiKey}";
            var response = await _httpClient.PostAsync(urlWithApiKey, content);

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
            var urlWithApiKey = $"codes/{code}?api_key={_apiKey}";
            var response = await _httpClient.GetAsync(urlWithApiKey);

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
            var urlWithApiKey = $"matches/by-code/{code}/ids?api_key={_apiKey}";
            var response = await _httpClient.GetAsync(urlWithApiKey);

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

