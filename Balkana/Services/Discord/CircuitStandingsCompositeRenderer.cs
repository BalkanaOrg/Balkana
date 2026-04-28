using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Balkana.Services.Discord
{
    /// <summary>Renders a PNG circuit standings summary with team logos for Discord.</summary>
    public sealed class CircuitStandingsCompositeRenderer
    {
        public const string AttachmentFilename = "circuit-standings.png";

        private const int CanvasWidth = 920;
        private const int LogoSize = 54;
        private const int Padding = 22;
        private const int MaxCanvasHeight = 10000;
        private const int MaxDetailLinesPerTeam = 22;

        private static readonly Color Bg = Color.ParseHex("#1a1a2e");
        private static readonly Color TextPrimary = Color.ParseHex("#eaeaea");
        private static readonly Color TextMuted = Color.ParseHex("#b8b8c8");
        private static readonly Color Accent = Color.ParseHex("#e94560");
        private static readonly Color LogoSlot = Color.ParseHex("#252540");

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CircuitStandingsCompositeRenderer> _logger;

        public CircuitStandingsCompositeRenderer(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment env,
            ILogger<CircuitStandingsCompositeRenderer> logger)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
            _logger = logger;
        }

        public async Task<byte[]?> RenderAsync(CircuitStandingsImageDto dto, CancellationToken cancellationToken = default)
        {
            var fontPath = System.IO.Path.Combine(_env.ContentRootPath, "Resources", "NotoSans-Regular.ttf");
            if (!File.Exists(fontPath))
            {
                _logger.LogError("Composite font missing at {Path}", fontPath);
                return null;
            }

            var fonts = new FontCollection();
            FontFamily family;
            try
            {
                family = fonts.Add(fontPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load font {Path}", fontPath);
                return null;
            }

            var fontTitle = family.CreateFont(24f, FontStyle.Bold);
            var fontTeam = family.CreateFont(16f, FontStyle.Bold);
            var fontDetail = family.CreateFont(13f, FontStyle.Regular);
            var fontFooter = family.CreateFont(12f, FontStyle.Regular);

            var logoCache = new Dictionary<int, Image<Rgba32>?>(capacity: 64);
            try
            {
                var http = _httpClientFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(12);

                foreach (var team in dto.Teams)
                {
                    if (logoCache.ContainsKey(team.TeamId))
                        continue;
                    var img = await TryLoadLogoAsync(http, team.LogoAbsoluteUrl, cancellationToken).ConfigureAwait(false);
                    logoCache[team.TeamId] = img;
                }

                var height = MeasureHeight(dto);
                height = Math.Clamp(height, 120, MaxCanvasHeight);

                using var canvas = new Image<Rgba32>(CanvasWidth, height, Bg);
                var y = (float)Padding;

                canvas.Mutate(c =>
                    c.DrawText($"{dto.GameFullName} — {dto.CircuitYear} circuit standings", fontTitle, TextPrimary,
                        new PointF(Padding, y)));
                y += 38;
                canvas.Mutate(c =>
                    c.Fill(Accent, new RectangularPolygon(Padding, y, CanvasWidth - 2 * Padding, 3)));
                y += 18;

                if (dto.Teams.Count == 0)
                {
                    canvas.Mutate(c =>
                        c.DrawText("No teams with tournament participation in this year.", fontDetail, TextMuted,
                            new PointF(Padding, y)));
                    y += 28;
                }

                foreach (var team in dto.Teams)
                {
                    var logoX = Padding;
                    var logoY = y + 4;
                    canvas.Mutate(c =>
                        c.Fill(LogoSlot, new RectangularPolygon(logoX, logoY, LogoSize, LogoSize)));

                    if (logoCache.TryGetValue(team.TeamId, out var logo) && logo != null)
                    {
                        using var sized = logo.Clone(ctx =>
                            ctx.Resize(new ResizeOptions
                            {
                                Size = new Size(LogoSize, LogoSize),
                                Mode = ResizeMode.Crop,
                                Position = AnchorPositionMode.Center
                            }));
                        canvas.Mutate(c => c.DrawImage(sized, new Point((int)logoX, (int)logoY), 1f));
                    }

                    var tx = Padding + LogoSize + 14;
                    var line1 = $"{team.Rank}. {team.FullName}  [{team.Tag}]";
                    canvas.Mutate(c =>
                        c.DrawText(TruncateForWidth(line1, 52), fontTeam, TextPrimary, new PointF(tx, y + 2)));

                    var line2 = $"{team.TotalPoints} pts total";
                    canvas.Mutate(c =>
                        c.DrawText(line2, fontDetail, TextMuted, new PointF(tx, y + 22)));

                    var detailY = y + 42;
                    foreach (var dl in team.DetailLines.Take(MaxDetailLinesPerTeam))
                    {
                        canvas.Mutate(c =>
                            c.DrawText(TruncateForWidth(dl, 95), fontDetail, TextMuted, new PointF(tx, detailY)));
                        detailY += 17;
                    }

                    var blockH = Math.Max(LogoSize + 8, (detailY - y) + 10);
                    y += blockH + 6;
                }

                y += 8;
                canvas.Mutate(c =>
                    c.DrawText("Balkana · Active roster only; substitutes excluded from CS player points.", fontFooter,
                        TextMuted, new PointF(Padding, y)));

                await using var ms = new MemoryStream();
                await canvas.SaveAsPngAsync(ms, cancellationToken).ConfigureAwait(false);
                return ms.ToArray();
            }
            finally
            {
                foreach (var kv in logoCache)
                    kv.Value?.Dispose();
            }
        }

        private static int MeasureHeight(CircuitStandingsImageDto dto)
        {
            var y = (float)Padding;
            y += 38 + 18;

            if (dto.Teams.Count == 0)
                y += 28;

            foreach (var team in dto.Teams)
            {
                var detailCount = Math.Min(team.DetailLines.Count, MaxDetailLinesPerTeam);
                var textBottom = 42 + detailCount * 17;
                var blockH = Math.Max(LogoSize + 8, textBottom + 10);
                y += blockH + 6;
            }

            y += 8 + 18 + Padding;
            return (int)Math.Ceiling(y);
        }

        private static string TruncateForWidth(string s, int maxChars)
        {
            if (s.Length <= maxChars)
                return s;
            return s[..(maxChars - 3)] + "...";
        }

        private async Task<Image<Rgba32>?> TryLoadLogoAsync(
            HttpClient http,
            string absoluteUrl,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(absoluteUrl)
                || !Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return null;

            try
            {
                using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return await Image.LoadAsync<Rgba32>(stream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Skipping logo {Url}", absoluteUrl);
                return null;
            }
        }
    }

    /// <summary>Input for <see cref="CircuitStandingsCompositeRenderer"/>.</summary>
    public sealed class CircuitStandingsImageDto
    {
        public string GameFullName { get; set; } = "";
        public int CircuitYear { get; set; }
        public List<CircuitStandingsImageTeamRow> Teams { get; set; } = new();
    }

    public sealed class CircuitStandingsImageTeamRow
    {
        public int Rank { get; set; }
        public int TeamId { get; set; }
        public string Tag { get; set; } = "";
        public string FullName { get; set; } = "";
        public string LogoAbsoluteUrl { get; set; } = "";
        public int TotalPoints { get; set; }
        public List<string> DetailLines { get; set; } = new();
    }
}
