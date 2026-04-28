using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Balkana.Data;
using Balkana.Models.Discord;
using Balkana.Services.Teams;
using Balkana.Services.Teams.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Balkana.Services.Discord
{
    public interface IDiscordCircuitStandingsService
    {
        Task<string?> BuildPlainTextPreviewAsync(
            string gameFullName,
            int circuitYear,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? ErrorMessage)> PostCircuitStandingsAsync(
            string gameFullName,
            int circuitYear,
            string? discordChannelIdOverride,
            CancellationToken cancellationToken = default);
    }

    public sealed class DiscordCircuitStandingsService : IDiscordCircuitStandingsService
    {
        /// <summary>Discord message content max length (legacy text split; preview still uses plain text).</summary>
        private const int MaxContentLength = 2000;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly JsonSerializerOptions PayloadJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ApplicationDbContext _context;
        private readonly ITeamService _teamService;
        private readonly DiscordConfig _discordConfig;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DiscordCircuitStandingsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly CircuitStandingsCompositeRenderer _compositeRenderer;

        public DiscordCircuitStandingsService(
            ApplicationDbContext context,
            ITeamService teamService,
            IOptions<DiscordConfig> discordConfig,
            HttpClient httpClient,
            ILogger<DiscordCircuitStandingsService> logger,
            IConfiguration configuration,
            CircuitStandingsCompositeRenderer compositeRenderer)
        {
            _context = context;
            _teamService = teamService;
            _discordConfig = discordConfig.Value;
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _compositeRenderer = compositeRenderer;

            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization")
                && !string.IsNullOrEmpty(_discordConfig.BotToken))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                    "Authorization",
                    $"Bot {_discordConfig.BotToken}");
            }
        }

        public Task<string?> BuildPlainTextPreviewAsync(
            string gameFullName,
            int circuitYear,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var standings = _teamService.GetCircuitStandings(gameFullName, circuitYear);
            var text = FormatStandingsPlainText(gameFullName, circuitYear, standings);
            return Task.FromResult<string?>(text);
        }

        public async Task<(bool Success, string? ErrorMessage)> PostCircuitStandingsAsync(
            string gameFullName,
            int circuitYear,
            string? discordChannelIdOverride,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_discordConfig.BotToken))
                return (false, "Discord BotToken is not configured.");

            var game = await _context.Games
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.FullName == gameFullName, cancellationToken);
            if (game == null)
                return (false, $"Game '{gameFullName}' not found.");

            var channelId = discordChannelIdOverride?.Trim();
            if (string.IsNullOrEmpty(channelId))
            {
                var mapping = await _context.DiscordGameResultChannels
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.GameId == game.Id && x.IsActive, cancellationToken);
                if (mapping == null)
                    return (false, $"No active Discord channel mapping for game '{game.FullName}'. Add one under Admin or pass a channel override.");
                channelId = mapping.DiscordChannelId;
            }

            if (!ulong.TryParse(channelId, out _))
                return (false, "Discord channel id must be a numeric snowflake.");

            var baseUrl = DiscordUrlHelper.GetBaseUrl(_configuration);
            var standings = _teamService.GetCircuitStandings(gameFullName, circuitYear);
            var imageDto = BuildCircuitStandingsImageDto(gameFullName, circuitYear, standings, baseUrl);

            var pngBytes = await _compositeRenderer.RenderAsync(imageDto, cancellationToken).ConfigureAwait(false);
            if (pngBytes == null || pngBytes.Length == 0)
                return (false, "Failed to render circuit standings image (check server logs and Resources/NotoSans-Regular.ttf).");

            var teamsUrl =
                $"{baseUrl}/Teams?Game={Uri.EscapeDataString(gameFullName)}&Year={circuitYear}";

            var embeds = new List<DiscordApiEmbed>
            {
                new DiscordApiEmbed
                {
                    Title = $"{gameFullName} — {circuitYear} circuit standings",
                    Url = teamsUrl,
                    Color = 0xE94560,
                    Image = new DiscordApiEmbedImage
                    {
                        Url = $"attachment://{CircuitStandingsCompositeRenderer.AttachmentFilename}"
                    },
                    Footer = new DiscordApiEmbedFooter { Text = "Balkana" }
                }
            };

            var payload = new DiscordCreateMultipartPayload
            {
                Embeds = embeds,
                Attachments =
                [
                    new DiscordApiAttachmentStub
                    {
                        Id = 0,
                        Filename = CircuitStandingsCompositeRenderer.AttachmentFilename
                    }
                ]
            };

            var url = $"https://discord.com/api/v10/channels/{channelId}/messages";
            var payloadJson = JsonSerializer.Serialize(payload, PayloadJsonOptions);

            using var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent(payloadJson, Encoding.UTF8, "application/json"), "payload_json");
            var fileContent = new ByteArrayContent(pngBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            multipart.Add(fileContent, "files[0]", CircuitStandingsCompositeRenderer.AttachmentFilename);

            var response = await _httpClient.PostAsync(url, multipart, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Discord API error {Status} posting circuit standings: {Body}", response.StatusCode,
                    body);
                return (false, $"Discord API {(int)response.StatusCode}: {body}");
            }

            return (true, null);
        }

        private static CircuitStandingsImageDto BuildCircuitStandingsImageDto(
            string gameFullName,
            int circuitYear,
            IReadOnlyList<CircuitStandingTeamDto> standings,
            string baseUrl)
        {
            baseUrl = baseUrl.TrimEnd('/');
            var dto = new CircuitStandingsImageDto
            {
                GameFullName = gameFullName,
                CircuitYear = circuitYear
            };

            var rank = 1;
            foreach (var t in standings)
            {
                dto.Teams.Add(new CircuitStandingsImageTeamRow
                {
                    Rank = rank++,
                    TeamId = t.TeamId,
                    Tag = t.Tag,
                    FullName = t.FullName,
                    LogoAbsoluteUrl = TournamentDiscordResultsBuilder.ToAbsoluteUrl(baseUrl, t.LogoURL),
                    TotalPoints = t.TotalPoints,
                    DetailLines = BuildDetailLinesForImage(t)
                });
            }

            return dto;
        }

        private static List<string> BuildDetailLinesForImage(CircuitStandingTeamDto t)
        {
            var lines = new List<string>();

            if (t.IsLeagueOfLegends)
            {
                lines.Add($"Placement (team): {t.LolTeamPlacementPoints ?? 0}");
                if (t.LolRoster.Count == 0)
                {
                    lines.Add("No active roster entries.");
                }
                else
                {
                    foreach (var line in t.LolRoster)
                    {
                        var role = string.IsNullOrEmpty(line.PositionName)
                            ? (line.PositionId?.ToString() ?? "?")
                            : line.PositionName;
                        lines.Add($"{role}: {line.Nickname}");
                    }
                }
            }
            else
            {
                lines.Add($"Roster pts: {t.CsRosterPlayerPoints ?? 0} · Org: {t.CsOrganisationPoints ?? 0}");
                if (t.CsPlayers.Count == 0)
                {
                    lines.Add("No active roster entries.");
                }
                else
                {
                    foreach (var p in t.CsPlayers)
                    {
                        var role = string.IsNullOrEmpty(p.PositionName)
                            ? (p.PositionId?.ToString() ?? "?")
                            : p.PositionName;
                        lines.Add($"{p.Nickname} ({role}) — {p.PointsThisYear} pts");
                    }
                }
            }

            return lines;
        }

        internal static string FormatStandingsPlainText(
            string gameFullName,
            int circuitYear,
            IReadOnlyList<CircuitStandingTeamDto> standings)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**{gameFullName} — {circuitYear} circuit standings**");
            sb.AppendLine();

            if (standings.Count == 0)
            {
                sb.AppendLine("_No teams with tournament participation in this year._");
                return sb.ToString();
            }

            var rank = 1;
            foreach (var team in standings)
            {
                AppendTeamBlock(sb, rank++, team);
                sb.AppendLine();
            }

            sb.AppendLine("_Balkana · Active roster only; substitutes excluded from CS player points._");
            return sb.ToString().TrimEnd();
        }

        private static void AppendTeamBlock(StringBuilder sb, int rank, CircuitStandingTeamDto team)
        {
            sb.AppendLine($"**{rank}. {EscapeDiscord(team.FullName)}** [{EscapeDiscord(team.Tag)}] — **{team.TotalPoints}** pts");

            if (team.IsLeagueOfLegends)
            {
                sb.AppendLine($"Placement (team): **{team.LolTeamPlacementPoints ?? 0}**");
                if (team.LolRoster.Count > 0)
                {
                    foreach (var line in team.LolRoster)
                    {
                        var role = string.IsNullOrEmpty(line.PositionName)
                            ? (line.PositionId?.ToString() ?? "?")
                            : line.PositionName;
                        sb.AppendLine($"• **{EscapeDiscord(role)}**: {EscapeDiscord(line.Nickname)}");
                    }
                }
                else
                {
                    sb.AppendLine("_No active roster entries._");
                }
            }
            else
            {
                sb.AppendLine(
                    $"Roster points: **{team.CsRosterPlayerPoints ?? 0}** · Org: **{team.CsOrganisationPoints ?? 0}**");
                if (team.CsPlayers.Count > 0)
                {
                    foreach (var p in team.CsPlayers)
                    {
                        var role = string.IsNullOrEmpty(p.PositionName)
                            ? (p.PositionId?.ToString() ?? "?")
                            : p.PositionName;
                        sb.AppendLine(
                            $"• {EscapeDiscord(p.Nickname)} ({EscapeDiscord(role)}) — **{p.PointsThisYear}** pts");
                    }
                }
                else
                {
                    sb.AppendLine("_No active roster entries._");
                }
            }
        }

        private static string EscapeDiscord(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\", "\\\\").Replace("`", "\\`").Replace("*", "\\*").Replace("_", "\\_").Replace("~", "\\~");
        }

        /// <summary>Splits text into ≤2000-char chunks, preferring newline boundaries.</summary>
        internal static IReadOnlyList<string> SplitIntoDiscordMessages(string fullText)
        {
            fullText = fullText.Replace("\r\n", "\n");
            if (fullText.Length <= MaxContentLength)
                return new[] { fullText };

            var chunks = new List<string>();
            var remaining = fullText;
            while (remaining.Length > 0)
            {
                if (remaining.Length <= MaxContentLength)
                {
                    chunks.Add(remaining);
                    break;
                }

                var take = MaxContentLength;
                var slice = remaining[..take];
                var lastNl = slice.LastIndexOf('\n');
                if (lastNl > MaxContentLength / 2)
                    take = lastNl + 1;

                chunks.Add(remaining[..take].TrimEnd());
                remaining = remaining[take..].TrimStart('\n');
            }

            for (var i = 1; i < chunks.Count; i++)
                chunks[i] = $"*(continued {i + 1}/{chunks.Count})*\n" + chunks[i];

            return chunks;
        }
    }
}
