namespace Balkana.Data.DTOs.Riot
{
    /// <summary>
    /// Request DTO for registering a tournament provider with Riot
    /// </summary>
    public class RiotProviderRegistrationDto
    {
        public string region { get; set; } // e.g., "EUW1"
        public string url { get; set; } // callback URL (required but can be empty string)
    }

    /// <summary>
    /// Response DTO containing provider ID
    /// </summary>
    public class RiotProviderRegistrationResponseDto
    {
        public int ProviderId { get; set; }
    }
}

