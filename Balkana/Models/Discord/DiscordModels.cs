namespace Balkana.Models.Discord
{
    public class DiscordCommandRequest
    {
        public string Command { get; set; } = string.Empty;
        public string[] Arguments { get; set; } = Array.Empty<string>();
    }

    public class DiscordCommandResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class DiscordCommandViewModel
    {
        public string Command { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
    }

    public class DiscordPlayerInfo
    {
        public string Nickname { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    public class DiscordTeamInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public List<DiscordPlayerInfo> Players { get; set; } = new();
    }
}
