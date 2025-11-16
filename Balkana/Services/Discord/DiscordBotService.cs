using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;

namespace Balkana.Services.Discord
{
    public interface IDiscordBotService
    {
        Task<string> ProcessCommandAsync(string command, string[] arguments);
        Task<bool> RegisterSlashCommandsAsync();
    }

    public class DiscordBotService : IDiscordBotService
    {
        private readonly ApplicationDbContext _context;
        private readonly DiscordConfig _discordConfig;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DiscordBotService> _logger;
        private readonly IConfiguration _configuration;
        
        // Cache for emojis to avoid repeated API calls
        private static Dictionary<string, string>? _emojiCache;
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1); // Cache for 1 hour

        public DiscordBotService(ApplicationDbContext context, IOptions<DiscordConfig> discordConfig, HttpClient httpClient, ILogger<DiscordBotService> logger, IConfiguration configuration)
        {
            _context = context;
            _discordConfig = discordConfig.Value;
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            // Set the authorization header
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bot {_discordConfig.BotToken}");
        }

        public async Task<string> ProcessCommandAsync(string command, string[] arguments)
        {
            try
            {
                switch (command.ToLower())
                {
                    case "team":
                        return await HandleTeamCommand(arguments);
                    case "player":
                        return await HandlePlayerCommand(arguments);
                    case "transfers":
                        return await HandleTransfersCommand(arguments);
                    case "bracket":
                        return await HandleBracketCommand(arguments);
                    default:
                        return "❌ Unknown command. Available commands: `/team`, `/player`, `/transfers`, `/bracket`";
                }
            }
            catch (Exception ex)
            {
                return $"❌ Error executing command: {ex.Message}";
            }
        }

        private async Task<string> HandleTeamCommand(string[] arguments)
        {
            if (arguments.Length == 0)
            {
                return "❌ Usage: `/team <team_tag_or_name>`\nExample: `/team TDI` or `/team Team Diamond`";
            }

            var searchTerm = arguments[0];

            // Find the team by tag or full name
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Tag.ToUpper() == searchTerm.ToUpper() || 
                                         t.FullName.ToUpper().Contains(searchTerm.ToUpper()));

            if (team == null)
            {
                return $"❌ Team '{searchTerm}' not found.";
            }

            // Get team emoji (bot's custom emoji with fallback to Unicode)
            var teamEmoji = await GetTeamEmojiAsync(team.Tag, team.FullName);

            var today = DateTime.Today;

            // Get active players (EndDate is null)
            var activePlayers = await _context.PlayerTeamTransfers
                .Include(pt => pt.Player)
                .Include(pt => pt.TeamPosition)
                .Where(pt => pt.TeamId == team.Id && pt.EndDate == null)
                .OrderBy(pt => pt.TeamPosition.Name)
                .Select(pt => new DiscordPlayerInfo
                {
                    Nickname = pt.Player.Nickname,
                    FullName = $"{pt.Player.FirstName} {pt.Player.LastName}",
                    Position = pt.TeamPosition.Name,
                    JoinedAt = pt.StartDate
                })
                .ToListAsync();

            // Get benched players (EndDate is not null but >= today)
            var benchedPlayers = await _context.PlayerTeamTransfers
                .Include(pt => pt.Player)
                .Include(pt => pt.TeamPosition)
                .Where(pt => pt.TeamId == team.Id && pt.EndDate != null && pt.EndDate >= today)
                .OrderBy(pt => pt.TeamPosition.Name)
                .Select(pt => new DiscordPlayerInfo
                {
                    Nickname = pt.Player.Nickname,
                    FullName = $"{pt.Player.FirstName} {pt.Player.LastName}",
                    Position = pt.TeamPosition.Name,
                    JoinedAt = pt.StartDate
                })
                .ToListAsync();

            var result = $"{teamEmoji} **{team.FullName} ({team.Tag})**\n";

            if (activePlayers.Any())
            {
                result += "\n**🟢 Active Roster:**\n";
                result += string.Join("\n", activePlayers.Select(p => 
                    $"• **{p.Nickname}** ({p.Position}) - {p.FullName}"));
            }

            if (benchedPlayers.Any())
            {
                result += "\n\n**🟡 Benched Players:**\n";
                result += string.Join("\n", benchedPlayers.Select(p => 
                    $"• **{p.Nickname}** ({p.Position}) - {p.FullName}"));
            }

            if (!activePlayers.Any() && !benchedPlayers.Any())
            {
                result += "\n❌ No players found.";
            }

            return result;
        }

        private async Task<string> HandlePlayerCommand(string[] arguments)
        {
            if (arguments.Length == 0)
            {
                return "❌ Usage: `/player <nickname>`\nExample: `/player ext1nct`";
            }

            var nickname = arguments[0];

            // Find the player by nickname
            var player = await _context.Players
                .Include(p => p.Nationality)
                .Include(p => p.GameProfiles)
                .FirstOrDefaultAsync(p => p.Nickname.ToLower() == nickname.ToLower());

            if (player == null)
            {
                return $"❌ Player '{nickname}' not found.";
            }

            // Get current team
            var currentTeam = await _context.PlayerTeamTransfers
                .Include(pt => pt.Team)
                .Include(pt => pt.TeamPosition)
                .Where(pt => pt.PlayerId == player.Id && pt.EndDate == null)
                .FirstOrDefaultAsync();

            var teamInfo = currentTeam != null 
                ? $"**Current Team:** {currentTeam.Team.FullName} ({currentTeam.Team.Tag}) - {currentTeam.TeamPosition.Name}"
                : "**Current Team:** Free Agent";

            var gameProfiles = player.GameProfiles.Any() 
                ? $"**Game Profiles:** {string.Join(", ", player.GameProfiles.Select(gp => gp.Provider))}"
                : "**Game Profiles:** None";

            var message = $"**{player.Nickname}**\n" +
                         $"**Full Name:** {player.FirstName} {player.LastName}\n" +
                         $"**Nationality:** {player.Nationality.Name}\n" +
                         $"{teamInfo}\n" +
                         $"{gameProfiles}";

            return message;
        }

        private async Task<string> HandleTransfersCommand(string[] arguments)
        {
            if (arguments.Length == 0)
            {
                return "❌ Usage: `/transfers <nickname>`\nExample: `/transfers ext1nct`";
            }

            var nickname = arguments[0];

            // Find the player by nickname
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Nickname.ToLower() == nickname.ToLower());

            if (player == null)
            {
                return $"❌ Player '{nickname}' not found.";
            }

            // Get all transfers for the player, ordered by start date (most recent first)
            // Filter out FreeAgent transfers
            var transfers = await _context.PlayerTeamTransfers
                .Include(pt => pt.Team)
                .Include(pt => pt.TeamPosition)
                .Where(pt => pt.PlayerId == player.Id && pt.Status != PlayerTeamStatus.FreeAgent)
                .OrderByDescending(pt => pt.StartDate)
                .ToListAsync();

            if (!transfers.Any())
            {
                return $"**{player.Nickname}**\n❌ No transfer history found.";
            }

            var result = $"**{player.Nickname} - Transfer History**\n\n";

            foreach (var transfer in transfers)
            {
                var startDate = transfer.StartDate.ToString("MMM dd, yyyy");
                var endDate = transfer.EndDate?.ToString("MMM dd, yyyy") ?? "Now";
                
                // Format position based on status
                var positionText = transfer.Status == PlayerTeamStatus.Benched 
                    ? $"{transfer.TeamPosition.Name} (Benched)"
                    : transfer.TeamPosition.Name;

                // Get team emoji for transfer history
                var transferTeamEmoji = await GetTeamEmojiAsync(transfer.Team.Tag, transfer.Team.FullName);
                result += $"{transferTeamEmoji} **{transfer.Team.FullName}**\n";
                result += $"• Position: {positionText}\n";
                result += $"• Period: {startDate} - {endDate}\n\n";
            }

            return result;
        }

        private async Task<string> HandleBracketCommand(string[] arguments)
        {
            if (arguments.Length == 0)
            {
                return "❌ Usage: `/bracket <tournament_name>`\nExample: `/bracket CS2 Spring Championship` or `/bracket Spring`";
            }

            var tournamentName = string.Join(" ", arguments);

            // Find the tournament by name (partial match)
            var tournament = await _context.Tournaments
                .Include(t => t.Series)
                .FirstOrDefaultAsync(t => t.FullName.ToLower().Contains(tournamentName.ToLower()) ||
                                         t.ShortName.ToLower().Contains(tournamentName.ToLower()));

            if (tournament == null)
            {
                return $"❌ Tournament '{tournamentName}' not found.";
            }

            // Check if bracket exists
            if (!tournament.Series.Any())
            {
                return $"❌ Bracket not yet generated for '{tournament.FullName}'.";
            }

            // For Discord, we need to return a special format that indicates an image should be sent
            // The Discord webhook controller will handle fetching and sending the actual image
            return $"BRACKET_IMAGE:{tournament.Id}:{tournament.FullName}";
        }

        /// <summary>
        /// Fetches emojis from Discord API and caches them
        /// </summary>
        private async Task<Dictionary<string, string>> FetchEmojisAsync()
        {
            // Check if cache is still valid
            if (_emojiCache != null && DateTime.UtcNow - _lastCacheUpdate < CacheExpiration)
            {
                return _emojiCache;
            }

            try
            {
                if (string.IsNullOrEmpty(_discordConfig.GuildId) || _discordConfig.GuildId == "YOUR_DISCORD_SERVER_ID")
                {
                    _logger.LogWarning("Discord GuildId not configured. Using fallback emojis.");
                    return new Dictionary<string, string>();
                }

                var response = await _httpClient.GetAsync($"https://discord.com/api/v10/guilds/{_discordConfig.GuildId}/emojis");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch emojis from Discord API. Status: {StatusCode}", response.StatusCode);
                    return new Dictionary<string, string>();
                }

                var emojis = await response.Content.ReadFromJsonAsync<DiscordEmoji[]>();
                
                if (emojis == null)
                {
                    _logger.LogWarning("No emojis found or failed to deserialize response.");
                    return new Dictionary<string, string>();
                }

                var emojiDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var emoji in emojis)
                {
                    if (emoji.Available != false) // Include available emojis (null means available)
                    {
                        var emojiString = emoji.Animated == true 
                            ? $"<a:{emoji.Name}:{emoji.Id}>" 
                            : $"<:{emoji.Name}:{emoji.Id}>";
                        
                        emojiDict[emoji.Name] = emojiString;
                        _logger.LogDebug("Cached emoji: {Name} -> {EmojiString}", emoji.Name, emojiString);
                    }
                }

                // Update cache
                _emojiCache = emojiDict;
                _lastCacheUpdate = DateTime.UtcNow;
                
                _logger.LogInformation("Successfully fetched and cached {Count} emojis from Discord", emojiDict.Count);
                return emojiDict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching emojis from Discord API");
                return _emojiCache ?? new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Gets the appropriate emoji for a team tag or full name.
        /// Uses bot's custom emojis uploaded to Discord Developer Portal.
        /// Format: <:TAG:id> or <:FULL_NAME:id>
        /// </summary>
        private async Task<string> GetTeamEmojiAsync(string teamTag, string? teamFullName = null)
        {
            try
            {
                // Fetch emojis from Discord API
                var emojis = await FetchEmojisAsync();

                // First try to find by team tag
                if (emojis.TryGetValue(teamTag, out var emoji))
                {
                    _logger.LogDebug("Found emoji for team tag '{TeamTag}': {Emoji}", teamTag, emoji);
                    return emoji;
                }

                // Then try to find by team full name
                if (!string.IsNullOrEmpty(teamFullName) && emojis.TryGetValue(teamFullName, out emoji))
                {
                    _logger.LogDebug("Found emoji for team full name '{TeamFullName}': {Emoji}", teamFullName, emoji);
                    return emoji;
                }

                _logger.LogDebug("No custom emoji found for team tag '{TeamTag}' or full name '{TeamFullName}', using Unicode fallback", teamTag, teamFullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching emoji for team tag '{TeamTag}'", teamTag);
            }

            // Fallback to Unicode emojis if no custom emoji found
            var unicodeEmoji = teamTag.ToUpper() switch
            {
                "TDI" => "💎", // Diamond
                "NAVI" => "🚀", // Navigation/Rocket
                "FAZE" => "⚡", // Lightning
                "G2" => "🎯", // Target
                "VITALITY" => "🐝", // Bee
                "HEROIC" => "🛡️", // Shield
                "ASTRA" => "⭐", // Star
                "LIQUID" => "💧", // Liquid drop
                "C9" => "☁️", // Cloud
                "TSM" => "🔥", // Fire
                "FNATIC" => "🦁", // Lion
                "NIP" => "🐺", // Wolf
                "VP" => "🐻", // Bear
                "SPIRIT" => "👻", // Spirit/Ghost
                "BIG" => "🐘", // Elephant
                "MOUZ" => "🐭", // Mouse
                "OG" => "🌊", // Ocean wave
                "ENCE" => "🦅", // Eagle
                "FURIA" => "🔥", // Fire
                "PAIN" => "💀", // Skull
                "MIBR" => "🇧🇷", // Brazil flag
                "LOUD" => "📢", // Loudspeaker
                "LEV" => "🏔️", // Mountain
                "KOI" => "🐟", // Fish
                "GLADIATORS" => "⚔️", // Crossed swords
                "SENTINELS" => "🛡️", // Shield
                "OPTIC" => "👁️", // Eye
                "100T" => "💯", // Hundred
                "NRG" => "⚡", // Lightning bolt
                "CLOUD9" => "☁️", // Cloud
                _ => "🏆" // Default trophy emoji
            };

            return unicodeEmoji;
        }

        public async Task<bool> RegisterSlashCommandsAsync()
        {
            try
            {
                var commands = new object[]
                {
                    new
                    {
                        name = "test",
                        description = "Test command to verify the bot is working"
                    },
                    new
                    {
                        name = "team",
                        description = "Get active and benched players for a team",
                        options = new[]
                        {
                            new
                            {
                                name = "team_tag_or_name",
                                description = "The team tag or name to search for",
                                type = 3, // STRING
                                required = true
                            }
                        }
                    },
                    new
                    {
                        name = "player",
                        description = "Get basic information for a player",
                        options = new[]
                        {
                            new
                            {
                                name = "nickname",
                                description = "The player's nickname",
                                type = 3, // STRING
                                required = true
                            }
                        }
                    },
                    new
                    {
                        name = "transfers",
                        description = "Get transfer history for a player",
                        options = new[]
                        {
                            new
                            {
                                name = "nickname",
                                description = "The player's nickname",
                                type = 3, // STRING
                                required = true
                            }
                        }
                    },
                    new
                    {
                        name = "bracket",
                        description = "Get bracket image for a tournament",
                        options = new[]
                        {
                            new
                            {
                                name = "tournament_name",
                                description = "The tournament name (partial match supported)",
                                type = 3, // STRING
                                required = true
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(commands);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(
                    $"https://discord.com/api/v10/applications/{_discordConfig.ClientId}/commands",
                    content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering slash commands: {ex.Message}");
                return false;
            }
        }
    }
}
