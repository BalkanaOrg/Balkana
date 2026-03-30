using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Balkana.Services.Discord
{
    /// <summary>
    /// Renders a PNG placement summary with team logos for Discord (uploaded as a message attachment).
    /// </summary>
    public class TournamentResultsCompositeRenderer
    {
        public const string AttachmentFilename = "tournament-results.png";

        private const int CanvasWidth = 920;
        private const int LogoSize = 54;
        private const int Padding = 22;
        private const int RowHeightNoEs = 68;
        private const int RowHeightWithEs = 84;
        private const int MaxCanvasHeight = 10000;

        private static readonly Color Bg = Color.ParseHex("#1a1a2e");
        private static readonly Color TextPrimary = Color.ParseHex("#eaeaea");
        private static readonly Color TextMuted = Color.ParseHex("#b8b8c8");
        private static readonly Color Accent = Color.ParseHex("#e94560");
        private static readonly Color LogoSlot = Color.ParseHex("#252540");

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<TournamentResultsCompositeRenderer> _logger;

        public TournamentResultsCompositeRenderer(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment env,
            ILogger<TournamentResultsCompositeRenderer> logger)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
            _logger = logger;
        }

        public async Task<byte[]?> RenderAsync(TournamentDiscordResultsDto dto, CancellationToken cancellationToken = default)
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
            var fontBand = family.CreateFont(18.5f, FontStyle.Bold);
            var fontTeam = family.CreateFont(16f, FontStyle.Bold);
            var fontDetail = family.CreateFont(13f, FontStyle.Regular);

            var logoCache = new Dictionary<int, Image<Rgba32>?>(capacity: 64);
            try
            {
                var http = _httpClientFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(12);

                foreach (var band in dto.Bands)
                {
                    foreach (var team in band.Teams)
                    {
                        if (logoCache.ContainsKey(team.TeamId))
                            continue;
                        var img = await TryLoadLogoAsync(http, team.LogoAbsoluteUrl, cancellationToken).ConfigureAwait(false);
                        logoCache[team.TeamId] = img;
                    }
                }

                var height = MeasureHeight(dto);
                height = Math.Clamp(height, 120, MaxCanvasHeight);

                using var canvas = new Image<Rgba32>(CanvasWidth, height, Bg);
                var y = (float)Padding;

                canvas.Mutate(c =>
                {
                    c.DrawText(dto.TournamentName + " — Results", fontTitle, TextPrimary, new PointF(Padding, y));
                });
                y += 38;
                canvas.Mutate(c =>
                    c.Fill(Accent, new RectangularPolygon(Padding, y, CanvasWidth - 2 * Padding, 3)));
                y += 18;

                foreach (var band in dto.Bands)
                {
                    canvas.Mutate(c =>
                        c.DrawText(band.Label, fontBand, Accent, new PointF(Padding, y)));
                    y += 30;

                    foreach (var team in band.Teams)
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
                        var orgPart = team.OrganisationPointsAwarded > 0
                            ? $" · Org {team.OrganisationPointsAwarded}"
                            : "";
                        var line1 = team.FullName;
                        canvas.Mutate(c =>
                            c.DrawText(line1, fontTeam, TextPrimary, new PointF(tx, y + 2)));
                        var line2 = $"Points: {team.PointsAwarded}{orgPart}";
                        canvas.Mutate(c =>
                            c.DrawText(line2, fontDetail, TextMuted, new PointF(tx, y + 22)));

                        var participants = team.ParticipantNicknames.Count > 0
                            ? string.Join(", ", team.ParticipantNicknames)
                            : "—";
                        participants = TruncateForWidth(participants, 95);
                        var line3 = "Roster: " + participants;
                        canvas.Mutate(c =>
                            c.DrawText(line3, fontDetail, TextMuted, new PointF(tx, y + 40)));

                        if (team.EmergencySubstituteNicknames.Count > 0)
                        {
                            var esLine = "Emergency substitutes: " + string.Join(", ", team.EmergencySubstituteNicknames);
                            esLine = TruncateForWidth(esLine, 95);
                            canvas.Mutate(c =>
                                c.DrawText(esLine, fontDetail, TextMuted, new PointF(tx, y + 56)));
                        }

                        y += TeamBlockHeight(team);
                    }

                    y += 10;
                }

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

        private static int TeamBlockHeight(DiscordPlacementTeamDto team) =>
            team.EmergencySubstituteNicknames.Count > 0 ? RowHeightWithEs : RowHeightNoEs;

        private static int MeasureHeight(TournamentDiscordResultsDto dto)
        {
            var h = Padding;
            h += 38 + 18;
            foreach (var band in dto.Bands)
            {
                h += 30 + band.Teams.Sum(TeamBlockHeight) + 10;
            }

            h += Padding;
            return h;
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
}
