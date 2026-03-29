using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Balkana.Data;
using Balkana.Models.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Balkana.Services.Discord
{
    public interface IDiscordTournamentResultsService
    {
        Task<(bool Success, string? ErrorMessage)> PostTournamentResultsAsync(
            int tournamentId,
            string? discordChannelIdOverride,
            CancellationToken cancellationToken = default);

        Task<string?> BuildPlainTextPreviewAsync(int tournamentId, CancellationToken cancellationToken = default);
    }

    public class DiscordTournamentResultsService : IDiscordTournamentResultsService
    {
        private const int MaxFieldValueLength = 1024;
        private const int MaxFieldsPerEmbed = 25;

        private readonly ApplicationDbContext _context;
        private readonly DiscordConfig _discordConfig;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DiscordTournamentResultsService> _logger;

        public DiscordTournamentResultsService(
            ApplicationDbContext context,
            IOptions<DiscordConfig> discordConfig,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<DiscordTournamentResultsService> logger)
        {
            _context = context;
            _discordConfig = discordConfig.Value;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;

            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization")
                && !string.IsNullOrEmpty(_discordConfig.BotToken))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                    "Authorization",
                    $"Bot {_discordConfig.BotToken}");
            }
        }

        public async Task<string?> BuildPlainTextPreviewAsync(int tournamentId, CancellationToken cancellationToken = default)
        {
            var baseUrl = GetBaseUrl();
            var builder = new TournamentDiscordResultsBuilder(_context);
            var dto = await builder.BuildAsync(tournamentId, baseUrl, cancellationToken);
            if (dto == null)
                return null;

            return FormatPlainText(dto, baseUrl);
        }

        public async Task<(bool Success, string? ErrorMessage)> PostTournamentResultsAsync(
            int tournamentId,
            string? discordChannelIdOverride,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_discordConfig.BotToken))
                return (false, "Discord BotToken is not configured.");

            var baseUrl = GetBaseUrl();
            var builder = new TournamentDiscordResultsBuilder(_context);
            var dto = await builder.BuildAsync(tournamentId, baseUrl, cancellationToken);
            if (dto == null)
                return (false, "Tournament not found.");

            var channelId = discordChannelIdOverride?.Trim();
            if (string.IsNullOrEmpty(channelId))
            {
                var mapping = await _context.DiscordGameResultChannels
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.GameId == dto.GameId && x.IsActive,
                        cancellationToken);
                if (mapping == null)
                    return (false, $"No active Discord channel mapping for game id {dto.GameId}. Add one under Admin or pass a channel override.");
                channelId = mapping.DiscordChannelId;
            }

            if (!ulong.TryParse(channelId, out _))
                return (false, "Discord channel id must be a numeric snowflake.");

            var embeds = BuildEmbeds(dto, baseUrl);
            var payload = new DiscordCreateMessageRequest { Embeds = embeds };

            var url = $"https://discord.com/api/v10/channels/{channelId}/messages";
            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Discord API error {Status}: {Body}", response.StatusCode, body);
                return (false, $"Discord API {(int)response.StatusCode}: {body}");
            }

            return (true, null);
        }

        private string GetBaseUrl()
        {
            var url = _configuration["BaseUrl"]?.Trim() ?? "https://balkana.org";
            return url.TrimEnd('/');
        }

        private static string FormatPlainText(TournamentDiscordResultsDto dto, string baseUrl)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# {dto.TournamentName} Results");
            sb.AppendLine($"{baseUrl}/Tournaments/Details/{dto.TournamentId}");
            sb.AppendLine();

            foreach (var band in dto.Bands)
            {
                sb.AppendLine($"## {band.TierEmoji} {band.Label}");
                foreach (var t in band.Teams)
                {
                    sb.AppendLine($"- **{t.FullName}** ({t.Tag}) — {t.PointsAwarded} pts (org: {t.OrganisationPointsAwarded})");
                    sb.AppendLine($"  Logo: {t.LogoAbsoluteUrl}");
                    sb.AppendLine($"  Participants: {(t.ParticipantNicknames.Count > 0 ? string.Join(", ", t.ParticipantNicknames) : "—")}");
                    sb.AppendLine($"  Emergency substitutes: {(t.EmergencySubstituteNicknames.Count > 0 ? string.Join(", ", t.EmergencySubstituteNicknames) : "—")}");
                }

                sb.AppendLine();
            }

            if (dto.Mvp != null)
            {
                sb.AppendLine("## ⭐ MVP");
                sb.AppendLine(FormatAwardLine(dto.Mvp));
                sb.AppendLine();
            }

            if (dto.Evps.Count > 0)
            {
                sb.AppendLine("## ✨ EVP");
                foreach (var e in dto.Evps)
                    sb.AppendLine("- " + FormatAwardLine(e));
            }

            return sb.ToString();
        }

        private static string FormatAwardLine(DiscordAwardPlayerDto p)
        {
            var team = p.TeamName != null
                ? $" · [{p.TeamName}]({p.TeamDetailsUrl})"
                : "";
            var logo = !string.IsNullOrEmpty(p.TeamLogoAbsoluteUrl)
                ? $" [logo]({p.TeamLogoAbsoluteUrl})"
                : "";
            return $"[{p.Nickname}]({p.PlayerProfileUrl}){team}{logo}";
        }

        private List<DiscordApiEmbed> BuildEmbeds(TournamentDiscordResultsDto dto, string baseUrl)
        {
            var tournamentUrl = $"{baseUrl}/Tournaments/Details/{dto.TournamentId}";
            var fields = new List<DiscordApiEmbedField>();

            foreach (var band in dto.Bands)
            {
                var value = string.Join(
                    "\n\n",
                    band.Teams.Select(t => FormatTeamBlock(t)));
                value = TruncateFieldValue(value);
                fields.Add(new DiscordApiEmbedField
                {
                    Name = TruncateFieldName($"{band.TierEmoji} {band.Label}"),
                    Value = value,
                    Inline = false
                });
            }

            if (dto.Mvp != null)
            {
                fields.Add(new DiscordApiEmbedField
                {
                    Name = "⭐ MVP",
                    Value = TruncateFieldValue(FormatAwardMarkdown(dto.Mvp)),
                    Inline = false
                });
            }

            if (dto.Evps.Count > 0)
            {
                var evpBody = string.Join("\n", dto.Evps.Select(FormatAwardMarkdown));
                fields.Add(new DiscordApiEmbedField
                {
                    Name = "✨ EVP",
                    Value = TruncateFieldValue(evpBody),
                    Inline = false
                });
            }

            fields.Add(new DiscordApiEmbedField
            {
                Name = "Tournament",
                Value = $"[{dto.TournamentName}]({tournamentUrl})",
                Inline = false
            });

            var embeds = new List<DiscordApiEmbed>();
            var chunks = fields.Chunk(MaxFieldsPerEmbed).ToList();
            for (var i = 0; i < chunks.Count; i++)
            {
                var embed = new DiscordApiEmbed
                {
                    Title = i == 0 ? $"{dto.TournamentName} Results" : $"{dto.TournamentName} Results (cont.)",
                    Url = i == 0 ? tournamentUrl : null,
                    Color = 0xE94560,
                    Fields = chunks[i].ToList()
                };
                if (i == chunks.Count - 1)
                    embed.Footer = new DiscordApiEmbedFooter { Text = "Balkana" };
                embeds.Add(embed);
            }

            return embeds;
        }

        private static string FormatTeamBlock(DiscordPlacementTeamDto t)
        {
            var orgPart = t.OrganisationPointsAwarded > 0
                ? $" · Org **{t.OrganisationPointsAwarded}**"
                : "";
            var participants = t.ParticipantNicknames.Count > 0
                ? string.Join(", ", t.ParticipantNicknames)
                : "—";
            var es = t.EmergencySubstituteNicknames.Count > 0
                ? string.Join(", ", t.EmergencySubstituteNicknames)
                : "—";
            return
                $"[**{EscapeMd(t.FullName)}** ({EscapeMd(t.Tag)})]({t.TeamDetailsUrl}) · [logo]({t.LogoAbsoluteUrl})\n" +
                $"Points: **{t.PointsAwarded}**{orgPart}\n" +
                $"Participants: {EscapeMd(participants)}\n" +
                $"Emergency substitutes: {EscapeMd(es)}";
        }

        private static string FormatAwardMarkdown(DiscordAwardPlayerDto p)
        {
            var line =
                $"[**{EscapeMd(p.Nickname)}**]({p.PlayerProfileUrl})";
            if (p.TeamDetailsUrl != null && p.TeamName != null)
                line += $" · [**{EscapeMd(p.TeamName)}**]({p.TeamDetailsUrl})";
            if (!string.IsNullOrEmpty(p.TeamLogoAbsoluteUrl))
                line += $" · [logo]({p.TeamLogoAbsoluteUrl})";
            return line;
        }

        private static string EscapeMd(string s) =>
            s.Replace("\\", "\\\\").Replace("]", "\\]").Replace("[", "\\[");

        private static string TruncateFieldValue(string value)
        {
            if (value.Length <= MaxFieldValueLength)
                return value;
            return value[..(MaxFieldValueLength - 3)] + "...";
        }

        private static string TruncateFieldName(string name)
        {
            const int max = 256;
            if (name.Length <= max)
                return name;
            return name[..(max - 3)] + "...";
        }
    }

    internal sealed class DiscordCreateMessageRequest
    {
        [JsonPropertyName("embeds")]
        public List<DiscordApiEmbed> Embeds { get; set; } = new();
    }

    internal sealed class DiscordApiEmbed
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("color")]
        public int Color { get; set; }

        [JsonPropertyName("fields")]
        public List<DiscordApiEmbedField> Fields { get; set; } = new();

        [JsonPropertyName("footer")]
        public DiscordApiEmbedFooter? Footer { get; set; }
    }

    internal sealed class DiscordApiEmbedField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("value")]
        public string Value { get; set; } = "";

        [JsonPropertyName("inline")]
        public bool Inline { get; set; }
    }

    internal sealed class DiscordApiEmbedFooter
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }
}
