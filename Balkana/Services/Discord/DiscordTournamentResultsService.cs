using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
        private const int MaxEmbedsPerMessage = 10;
        private const int MaxEmbedDescriptionLength = 4096;

        private const string MvpTrophyPath = "/uploads/Tournaments/Trophies/Balkana-MVP-icon.png";
        private const string EvpTrophyPath = "/uploads/Tournaments/Trophies/Balkana-EVP-icon.png";

        private static readonly JsonSerializerOptions PayloadJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ApplicationDbContext _context;
        private readonly DiscordConfig _discordConfig;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DiscordTournamentResultsService> _logger;
        private readonly TournamentResultsCompositeRenderer _compositeRenderer;

        public DiscordTournamentResultsService(
            ApplicationDbContext context,
            IOptions<DiscordConfig> discordConfig,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<DiscordTournamentResultsService> logger,
            TournamentResultsCompositeRenderer compositeRenderer)
        {
            _context = context;
            _discordConfig = discordConfig.Value;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _compositeRenderer = compositeRenderer;

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

            var pngBytes = await _compositeRenderer.RenderAsync(dto, cancellationToken).ConfigureAwait(false);
            if (pngBytes == null || pngBytes.Length == 0)
                return (false, "Failed to render tournament results image (check server logs and Resources/NotoSans-Regular.ttf).");

            var mvpIconUrl = $"{baseUrl}{MvpTrophyPath}";
            var evpIconUrl = $"{baseUrl}{EvpTrophyPath}";
            var embeds = BuildEmbeds(dto, baseUrl, mvpIconUrl, evpIconUrl);
            if (embeds.Count > MaxEmbedsPerMessage)
            {
                _logger.LogWarning(
                    "Tournament {TournamentId} would send {Count} embeds; truncating to {Max}.",
                    tournamentId,
                    embeds.Count,
                    MaxEmbedsPerMessage);
                embeds = embeds.Take(MaxEmbedsPerMessage).ToList();
            }

            var payload = new DiscordCreateMultipartPayload
            {
                Embeds = embeds,
                Attachments =
                [
                    new DiscordApiAttachmentStub
                    {
                        Id = 0,
                        Filename = TournamentResultsCompositeRenderer.AttachmentFilename
                    }
                ]
            };

            var url = $"https://discord.com/api/v10/channels/{channelId}/messages";
            var payloadJson = JsonSerializer.Serialize(payload, PayloadJsonOptions);

            using var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent(payloadJson, Encoding.UTF8, "application/json"), "payload_json");
            var fileContent = new ByteArrayContent(pngBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            multipart.Add(fileContent, "files[0]", TournamentResultsCompositeRenderer.AttachmentFilename);

            var response = await _httpClient.PostAsync(url, multipart, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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

        private static string TrophyUrlsFooter(string baseUrl) =>
            $"MVP trophy image: {baseUrl}{MvpTrophyPath}\nEVP trophy image: {baseUrl}{EvpTrophyPath}";

        private static string FormatPlainText(TournamentDiscordResultsDto dto, string baseUrl)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# {dto.TournamentName} Results");
            sb.AppendLine($"{baseUrl}/Tournaments/Details/{dto.TournamentId}");
            sb.AppendLine();
            sb.AppendLine("(On Discord, a composite PNG with team logos is attached to the message.)");
            sb.AppendLine();

            foreach (var band in dto.Bands)
            {
                sb.AppendLine($"## {band.TierEmoji} {band.Label}");
                foreach (var t in band.Teams)
                {
                    sb.AppendLine($"- **{t.FullName}** — {t.PointsAwarded} pts (org: {t.OrganisationPointsAwarded})");
                    sb.AppendLine($"  Logo: {t.LogoAbsoluteUrl}");
                    sb.AppendLine($"  Emergency substitutes: {(t.EmergencySubstituteNicknames.Count > 0 ? string.Join(", ", t.EmergencySubstituteNicknames) : "—")}");
                }

                sb.AppendLine();
            }

            if (dto.Mvp != null)
            {
                sb.AppendLine($"## MVP ({baseUrl}{MvpTrophyPath})");
                sb.AppendLine(FormatAwardLine(dto.Mvp));
                sb.AppendLine();
            }

            if (dto.Evps.Count > 0)
            {
                sb.AppendLine($"## EVP ({baseUrl}{EvpTrophyPath})");
                foreach (var e in dto.Evps)
                    sb.AppendLine("- " + FormatAwardLine(e));
            }

            sb.AppendLine();
            sb.AppendLine(TrophyUrlsFooter(baseUrl));

            return sb.ToString();
        }

        private static string FormatAwardLine(DiscordAwardPlayerDto p)
        {
            if (p.TeamName != null && p.TeamDetailsUrl != null)
                return $"[{p.TeamName}]({p.TeamDetailsUrl}) · [{p.Nickname}]({p.PlayerProfileUrl})";
            return $"[{p.Nickname}]({p.PlayerProfileUrl})";
        }

        private List<DiscordApiEmbed> BuildEmbeds(
            TournamentDiscordResultsDto dto,
            string baseUrl,
            string mvpIconUrl,
            string evpIconUrl)
        {
            var tournamentUrl = $"{baseUrl}/Tournaments/Details/{dto.TournamentId}";
            var attachmentRef = $"attachment://{TournamentResultsCompositeRenderer.AttachmentFilename}";

            var embeds = new List<DiscordApiEmbed>
            {
                new DiscordApiEmbed
                {
                    Title = $"{dto.TournamentName} Results",
                    Url = tournamentUrl,
                    Color = 0xE94560,
                    Image = new DiscordApiEmbedImage { Url = attachmentRef },
                    Fields = null
                }
            };

            if (dto.Mvp != null)
            {
                embeds.Add(new DiscordApiEmbed
                {
                    Author = new DiscordApiEmbedAuthor
                    {
                        Name = "MVP",
                        IconUrl = mvpIconUrl
                    },
                    Description = TruncateDescription(FormatAwardMarkdown(dto.Mvp)),
                    Color = 0xE94560
                });
            }

            if (dto.Evps.Count > 0)
            {
                var evpBody = string.Join("\n", dto.Evps.Select(FormatAwardMarkdown));
                embeds.Add(new DiscordApiEmbed
                {
                    Author = new DiscordApiEmbedAuthor
                    {
                        Name = "EVP",
                        IconUrl = evpIconUrl
                    },
                    Description = TruncateDescription(evpBody),
                    Color = 0xE94560
                });
            }

            if (embeds.Count > 0)
                embeds[^1].Footer = new DiscordApiEmbedFooter { Text = "Balkana" };

            return embeds;
        }

        private static string FormatAwardMarkdown(DiscordAwardPlayerDto p)
        {
            if (p.TeamDetailsUrl != null && p.TeamName != null)
                return $"[**{EscapeMd(p.TeamName)}**]({p.TeamDetailsUrl}) · [**{EscapeMd(p.Nickname)}**]({p.PlayerProfileUrl})";
            return $"[**{EscapeMd(p.Nickname)}**]({p.PlayerProfileUrl})";
        }

        private static string EscapeMd(string s) =>
            s.Replace("\\", "\\\\").Replace("]", "\\]").Replace("[", "\\[");

        private static string TruncateDescription(string value)
        {
            if (value.Length <= MaxEmbedDescriptionLength)
                return value;
            return value[..(MaxEmbedDescriptionLength - 20)] + "\n… (truncated)";
        }
    }

    internal sealed class DiscordCreateMultipartPayload
    {
        [JsonPropertyName("embeds")]
        public List<DiscordApiEmbed> Embeds { get; set; } = new();

        [JsonPropertyName("attachments")]
        public List<DiscordApiAttachmentStub> Attachments { get; set; } = new();
    }

    internal sealed class DiscordApiAttachmentStub
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = "";
    }

    internal sealed class DiscordApiEmbed
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("color")]
        public int Color { get; set; }

        [JsonPropertyName("author")]
        public DiscordApiEmbedAuthor? Author { get; set; }

        [JsonPropertyName("image")]
        public DiscordApiEmbedImage? Image { get; set; }

        [JsonPropertyName("fields")]
        public List<DiscordApiEmbedField>? Fields { get; set; }

        [JsonPropertyName("footer")]
        public DiscordApiEmbedFooter? Footer { get; set; }
    }

    internal sealed class DiscordApiEmbedAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }
    }

    internal sealed class DiscordApiEmbedImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
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
