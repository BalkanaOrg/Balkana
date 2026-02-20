using Balkana.Data;
using Balkana.Data.DTOs.Riot;
using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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
        private readonly string _routingCluster;

        public RiotTournamentService(HttpClient httpClient, ApplicationDbContext context, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;
            _configuration = configuration;
            // Trim to avoid newlines/whitespace from .env or config
            _apiKey = (_configuration["Riot:ApiKey"] ?? "").Trim();

            _routingCluster = ResolveRoutingCluster(_configuration["Riot:TournamentRegion"]);
            // Production tournament-v5 path is /lol/tournament/v5/ (per Riot docs)
            _httpClient.BaseAddress = new Uri($"https://{_routingCluster}.api.riotgames.com/lol/tournament/v5/");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            // User-Agent matching browser - some APIs return 403 for default/bot User-Agents
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-GB,en;q=0.7");
            
            // Use api_key query param (matches Riot Developer Portal "Try it")
            // X-Riot-Token header can also work, but portal uses api_key - try this first
            
            Console.WriteLine($"[RIOT TOURNAMENT API] Configured Base URL: {_httpClient.BaseAddress}");
            Console.WriteLine($"[RIOT TOURNAMENT API] Routing Cluster: {_routingCluster}");
            Console.WriteLine($"[RIOT TOURNAMENT API] API Key Length: {_apiKey?.Length ?? 0}");
            Console.WriteLine($"[RIOT TOURNAMENT API] API Key Format: {(_apiKey?.StartsWith("RGAPI-") == true ? "Valid RGAPI format" : "Invalid format")}");
        }

        /// <summary>Appends api_key to the path/query (Riot supports both api_key and X-Riot-Token).</summary>
        private string WithApiKey(string path)
        {
            var sep = path.Contains('?') ? "&" : "?";
            return $"{path}{sep}api_key={Uri.EscapeDataString(_apiKey)}";
        }

        public async Task<bool> TestApiKeyAsync()
        {
            try
            {
                var url = WithApiKey("providers");
                Console.WriteLine($"[RIOT TOURNAMENT API] Testing configured cluster {_routingCluster} via {_httpClient.BaseAddress}{url}");

                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"[RIOT TOURNAMENT API] API Key Test (GET providers) - Status: {response.StatusCode}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"[RIOT TOURNAMENT API] API Key is valid - providers endpoint accessible");
                    return true;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[RIOT TOURNAMENT API] Error Response: {errorContent}");

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine($"[RIOT TOURNAMENT API] 403 Forbidden - API key may lack tournament-v5 permissions or use wrong auth/URL");
                    return false;
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RIOT TOURNAMENT API] API Key Test Error: {ex.Message}");
                return false;
            }
        }

        public async Task<int> RegisterProviderAsync(string region, string callbackUrl = "")
        {
            // Tournament API expects platform ID (EUW1, NA1, EUN1, etc.) - do not strip the "1"
            var regionFormatted = region.Trim().ToUpperInvariant();

            var request = new RiotProviderRegistrationDto
            {
                region = regionFormatted,
                url = string.IsNullOrEmpty(callbackUrl) ? "" : callbackUrl  // Empty for testing
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var url = WithApiKey("providers");
            
            Console.WriteLine($"[RIOT TOURNAMENT API] POST {_httpClient.BaseAddress}{url}");
            Console.WriteLine($"[RIOT TOURNAMENT API] Original Region: {region.ToUpper()}, Formatted: {regionFormatted}");
            Console.WriteLine($"[RIOT TOURNAMENT API] Callback: {callbackUrl}");
            Console.WriteLine($"[RIOT TOURNAMENT API] Request Body: {JsonSerializer.Serialize(request)}");
            
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[RIOT TOURNAMENT API ERROR] Status: {response.StatusCode}");
                Console.WriteLine($"[RIOT TOURNAMENT API ERROR] Response: {error}");
                throw new Exception($"Failed to register provider: {response.StatusCode} - {error}");
            }

            var providerId = await response.Content.ReadFromJsonAsync<int>();
            Console.WriteLine($"[RIOT TOURNAMENT API] Successfully registered provider: {providerId}");
            return providerId;
        }

        private static string ResolveRoutingCluster(string? configuredValue)
        {
            var defaultCluster = "americas";

            if (string.IsNullOrWhiteSpace(configuredValue))
            {
                return defaultCluster;
            }

            var normalized = configuredValue.Trim().ToLowerInvariant();
            var allowedClusters = new HashSet<string> { "americas", "europe", "asia", "sea", "esports" };

            if (allowedClusters.Contains(normalized))
            {
                return normalized;
            }

            return normalized switch
            {
                "euw" or "euw1" or "eune" or "eune1" or "tr" or "tr1" or "ru" => "europe",
                "na" or "na1" or "br" or "br1" or "la1" or "la2" or "lan" or "las" or "oc1" => "americas",
                "kr" or "jp" or "jp1" => "asia",
                _ => defaultCluster
            };
        }

        public async Task<RiotTournament> CreateTournamentAsync(string name, int providerId, string region, int? internalTournamentId = null)
        {
            var request = new RiotTournamentRegistrationDto
            {
                providerId = providerId,
                name = name
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(WithApiKey("tournaments"), content);

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
            var response = await _httpClient.PostAsync(WithApiKey($"codes?tournamentId={riotTournamentId}&count={count}"), content);

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
            var response = await _httpClient.GetAsync(WithApiKey($"codes/{code}"));

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
            var response = await _httpClient.GetAsync(WithApiKey($"matches/by-code/{code}/ids"));

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

