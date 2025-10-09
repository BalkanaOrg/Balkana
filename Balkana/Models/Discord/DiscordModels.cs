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

    // Discord API Emoji Models
    public class DiscordEmoji
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public DiscordUser? User { get; set; }
        public bool? RequireColons { get; set; }
        public bool? Managed { get; set; }
        public bool? Animated { get; set; }
        public bool? Available { get; set; }
    }

    public class DiscordUser
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Discriminator { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public bool? Bot { get; set; }
        public bool? System { get; set; }
        public bool? MfaEnabled { get; set; }
        public string? Banner { get; set; }
        public int? AccentColor { get; set; }
        public string? Locale { get; set; }
        public bool? Verified { get; set; }
        public string? Email { get; set; }
        public int? Flags { get; set; }
        public int? PremiumType { get; set; }
        public int? PublicFlags { get; set; }
    }
}
