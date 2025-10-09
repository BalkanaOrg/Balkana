namespace Balkana.Models.Discord
{
    public class DiscordConfig
    {
        public string BotToken { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string GuildId { get; set; } = string.Empty; // The Discord server ID where emojis are uploaded
    }
}
