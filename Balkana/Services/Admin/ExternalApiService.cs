using Balkana.Data.DTOs.FaceIt;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace Balkana.Services.Admin
{
    public class ExternalApiService : IExternalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public ExternalApiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<List<FaceItPlayerDTO>> SearchFaceitPlayersAsync(string nickname)
        {
            var apiKey = _config["Faceit:ApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var url = $"https://open.faceit.com/data/v4/search/players?nickname={Uri.EscapeDataString(nickname)}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<FaceItPlayerDTO>();

            var json = await response.Content.ReadFromJsonAsync<FaceitSearchResponse>();
            return json?.Items?.Select(p => new FaceItPlayerDTO
            {
                player_id = p.PlayerId,
                nickname = p.Nickname,
                country = p.Country,
                avatar = p.Avatar
            }).ToList() ?? new List<FaceItPlayerDTO>();
        }


        public async Task<string?> SearchRiotPlayerAsync(string gameName, string tagLine, string region)
        {
            var apiKey = _config["Riot:ApiKey"];
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Riot-Token", apiKey);

            // Riot routing regions: americas | asia | europe | sea
            var url = $"https://{region}.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Riot API error: {response.StatusCode}, {err}");
            }

            var json = await response.Content.ReadFromJsonAsync<RiotAccountResponse>();
            return json?.Puuid; // use PUUID as the universal identifier
        }

        private class FaceitSearchResponse
        {
            public List<FaceitPlayer> Items { get; set; } = new();
        }

        private class FaceitPlayer
        {
            [JsonPropertyName("player_id")]
            public string PlayerId { get; set; } = "";

            [JsonPropertyName("nickname")]
            public string Nickname { get; set; } = "";

            [JsonPropertyName("country")]
            public string Country { get; set; } = "";

            [JsonPropertyName("avatar")]
            public string Avatar { get; set; } = "";
        }

        private class RiotAccountResponse
        {
            public string Puuid { get; set; } = "";
            public string GameName { get; set; } = "";
            public string TagLine { get; set; } = "";
        }
    }
}
