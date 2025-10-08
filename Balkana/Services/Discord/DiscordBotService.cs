using Balkana.Data;
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

        public DiscordBotService(ApplicationDbContext context, IOptions<DiscordConfig> discordConfig, HttpClient httpClient, ILogger<DiscordBotService> logger)
        {
            _context = context;
            _discordConfig = discordConfig.Value;
            _httpClient = httpClient;
            _logger = logger;

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
                    default:
                        return "❌ Unknown command. Available commands: `/team`, `/player`, `/transfers`";
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

            var result = $"**{team.FullName} ({team.Tag})**\n";

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
                .Where(pt => pt.PlayerId == player.Id && pt.Status.ToString() != "FreeAgent")
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
                var positionText = transfer.Status.ToString() == "Benched" 
                    ? $"{transfer.TeamPosition.Name} (Benched)"
                    : transfer.TeamPosition.Name;

                result += $"**{transfer.Team.FullName} ({transfer.Team.Tag})**\n";
                result += $"• Position: {positionText}\n";
                result += $"• Period: {startDate} - {endDate}\n\n";
            }

            return result;
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
