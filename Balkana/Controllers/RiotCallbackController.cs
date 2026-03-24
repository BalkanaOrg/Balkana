using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Services.Tournaments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Balkana.Controllers
{
    /// <summary>
    /// Receives match completion callbacks from Riot Tournament API.
    /// Endpoint must be registered as provider URL when creating a Riot tournament provider.
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [Route("api/riot")]
    public class RiotCallbackController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _db;
        private readonly IRiotTournamentService _riotService;

        public RiotCallbackController(
            IConfiguration config,
            ApplicationDbContext db,
            IRiotTournamentService riotService)
        {
            _config = config;
            _db = db;
            _riotService = riotService;
        }

        /// <summary>
        /// Callback URL for Riot to POST when a match completes.
        /// Validate via ?key=CallbackSecret in the registered URL.
        /// </summary>
        [HttpPost("callback")]
        public async Task<IActionResult> Callback(CancellationToken ct)
        {
            var secret = (_config["Riot:CallbackSecret"] ?? "").Trim();
            if (string.IsNullOrEmpty(secret))
                return StatusCode(500, "Callback secret not configured");

            var key = Request.Query["key"].FirstOrDefault() ?? "";
            if (key != secret)
                return Unauthorized();

            string rawBody;
            using (var reader = new StreamReader(Request.Body))
                rawBody = await reader.ReadToEndAsync(ct);

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                // Accept but log - some callbacks may be empty
                return Ok();
            }

            try
            {
                var matchIds = ExtractMatchIds(rawBody, out var tournamentCode);

                if (matchIds.Count == 0 && !string.IsNullOrWhiteSpace(tournamentCode))
                {
                    // Fallback: fetch by tournament code
                    try
                    {
                        var gameIds = await _riotService.GetMatchIdsByTournamentCodeAsync(tournamentCode);
                        var platform = DerivePlatformFromTournamentCode(tournamentCode);
                        foreach (var gid in gameIds)
                            matchIds.Add($"{platform}_{gid}");
                    }
                    catch (Exception ex)
                    {
                        await SaveFailedAsync(rawBody, ex.Message, ct);
                        return Ok(); // Still 200 so Riot doesn't retry
                    }
                }

                if (matchIds.Count == 0)
                {
                    // Store for debugging - we couldn't derive matchId
                    await SaveUnknownAsync(rawBody, ct);
                    return Ok();
                }

                foreach (var matchId in matchIds)
                {
                    await UpsertPendingMatchAsync(matchId, tournamentCode, rawBody, ct);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                await SaveFailedAsync(rawBody, ex.Message, ct);
                return Ok(); // 200 so Riot doesn't retry
            }
        }

        private static List<string> ExtractMatchIds(string json, out string? tournamentCode)
        {
            tournamentCode = null;
            var matchIds = new List<string>();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 1. metadata.matchId
            if (root.TryGetProperty("metadata", out var meta) && meta.TryGetProperty("matchId", out var m))
            {
                var s = m.GetString();
                if (!string.IsNullOrEmpty(s)) matchIds.Add(s);
            }

            // 2. info.matchId or root matchId
            if (matchIds.Count == 0)
            {
                if (root.TryGetProperty("info", out var info) && info.TryGetProperty("matchId", out var im))
                    TryAddMatchId(im.GetString(), matchIds);
                else if (root.TryGetProperty("matchId", out var rm))
                    TryAddMatchId(rm.GetString(), matchIds);
            }

            // 3. info.gameId + platform
            if (matchIds.Count == 0 && root.TryGetProperty("info", out var inf))
            {
                if (inf.TryGetProperty("gameId", out var g) && g.TryGetInt64(out var gameId))
                {
                    var platform = GetString(root, "platformId") ?? GetString(inf, "platformId")
                        ?? DerivePlatformFromTournamentCode(GetString(root, "tournamentCode") ?? GetString(inf, "tournamentCode"));
                    if (!string.IsNullOrEmpty(platform))
                        matchIds.Add($"{platform}_{gameId}");
                }
            }

            // 4. tournamentCode for later fallback
            tournamentCode = GetString(root, "tournamentCode");
            if (string.IsNullOrEmpty(tournamentCode) && root.TryGetProperty("info", out var infoEl))
                tournamentCode = infoEl.TryGetProperty("tournamentCode", out var tc) ? tc.GetString() : null;

            return matchIds;
        }

        private static string? GetString(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var p) ? p.GetString() : null;

        private static void TryAddMatchId(string? s, List<string> list)
        {
            if (!string.IsNullOrEmpty(s)) list.Add(s);
        }

        private static string DerivePlatformFromTournamentCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "EUN1";
            var first = code.Split('-')[0].Trim().ToUpperInvariant();
            if (first.Length >= 3) return first;
            return first switch
            {
                "EUNE" or "EUN" => "EUN1",
                "EUW" => "EUW1",
                "NA" => "NA1",
                "BR" => "BR1",
                "KR" => "KR",
                "JP" => "JP1",
                "TR" => "TR1",
                "RU" => "RU",
                "OCE" or "OC" => "OC1",
                "LAN" or "LA1" => "LA1",
                "LAS" or "LA2" => "LA2",
                _ => "EUN1"
            };
        }

        private async Task UpsertPendingMatchAsync(string matchId, string? tournamentCode, string rawPayload, CancellationToken ct)
        {
            var existing = await _db.RiotPendingMatches
                .FirstOrDefaultAsync(p => p.MatchId == matchId && p.Status == RiotPendingMatchStatus.Pending, ct);

            int? riotCodeId = null;
            if (!string.IsNullOrWhiteSpace(tournamentCode))
            {
                var code = await _db.RiotTournamentCodes.FirstOrDefaultAsync(tc => tc.Code == tournamentCode, ct);
                riotCodeId = code?.Id;
            }

            if (existing != null)
            {
                existing.RawPayload = rawPayload;
                existing.TournamentCode = tournamentCode;
                existing.RiotTournamentCodeId = riotCodeId;
            }
            else
            {
                _db.RiotPendingMatches.Add(new RiotPendingMatch
                {
                    MatchId = matchId,
                    TournamentCode = tournamentCode,
                    RiotTournamentCodeId = riotCodeId,
                    RawPayload = rawPayload.Length > 100000 ? rawPayload[..100000] + "...[truncated]" : rawPayload,
                    Status = RiotPendingMatchStatus.Pending
                });
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task SaveFailedAsync(string rawPayload, string error, CancellationToken ct)
        {
            _db.RiotPendingMatches.Add(new RiotPendingMatch
            {
                MatchId = "UNKNOWN",
                RawPayload = rawPayload.Length > 100000 ? rawPayload[..100000] + "...[truncated]" : rawPayload,
                Status = RiotPendingMatchStatus.Failed,
                ErrorMessage = error.Length > 500 ? error[..500] : error
            });
            await _db.SaveChangesAsync(ct);
        }

        private async Task SaveUnknownAsync(string rawPayload, CancellationToken ct)
        {
            _db.RiotPendingMatches.Add(new RiotPendingMatch
            {
                MatchId = "UNKNOWN",
                RawPayload = rawPayload.Length > 100000 ? rawPayload[..100000] + "...[truncated]" : rawPayload,
                Status = RiotPendingMatchStatus.Failed,
                ErrorMessage = "Could not derive matchId from callback payload"
            });
            await _db.SaveChangesAsync(ct);
        }
    }
}
