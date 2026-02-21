using Balkana.Data.DTOs.Riot;

namespace Balkana.Services.Riot
{
    public class RiotMatchApiService : IRiotMatchApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public RiotMatchApiService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<RiotMatchDto?> GetMatchAsync(string matchId)
        {
            var cluster = GetRoutingClusterForMatchId(matchId);
            var client = CreateClientForCluster(cluster);

            var response = await client.GetAsync($"lol/match/v5/matches/{matchId}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<RiotMatchDto>();
        }

        private static string GetRoutingClusterForMatchId(string matchId)
        {
            if (string.IsNullOrEmpty(matchId) || !matchId.Contains('_'))
                return "europe";

            var platform = matchId.Split('_')[0].Trim().ToLowerInvariant();
            return platform switch
            {
                "euw1" or "eune1" or "eun1" or "tr1" or "ru" => "europe",
                "na1" or "br1" or "la1" or "la2" or "oc1" => "americas",
                "kr" or "jp1" => "asia",
                "ph2" or "sg2" or "th2" or "tw2" or "vn2" => "sea",
                _ => "europe"
            };
        }

        private HttpClient CreateClientForCluster(string cluster)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri($"https://{cluster}.api.riotgames.com/");
            client.DefaultRequestHeaders.Add("X-Riot-Token", (_config["Riot:ApiKey"] ?? "").Trim());
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            return client;
        }
    }
}
