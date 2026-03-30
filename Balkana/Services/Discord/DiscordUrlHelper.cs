namespace Balkana.Services.Discord;

public static class DiscordUrlHelper
{
    public static string GetBaseUrl(IConfiguration configuration)
    {
        var url = configuration["BaseUrl"]?.Trim() ?? "https://balkana.org";
        return url.TrimEnd('/');
    }
}
