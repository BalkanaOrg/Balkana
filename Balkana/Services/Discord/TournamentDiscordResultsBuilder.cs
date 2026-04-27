using Balkana.Data;
using Balkana.Data.Infrastructure.Extensions;
using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Discord
{
    public class TournamentDiscordResultsBuilder
    {
        private readonly ApplicationDbContext _context;

        public TournamentDiscordResultsBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TournamentDiscordResultsDto?> BuildAsync(int tournamentId, string baseUrl, CancellationToken cancellationToken = default)
        {
            var tournament = await _context.Tournaments
                .AsNoTracking()
                .Include(t => t.Game)
                .Include(t => t.Placements)
                .ThenInclude(p => p.Team)
                .FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);

            if (tournament == null)
                return null;

            baseUrl = baseUrl.TrimEnd('/');
            var detailsSeg = string.IsNullOrWhiteSpace(tournament.ShortName) ? tournament.Id.ToString() : tournament.ShortName;

            var teamIds = tournament.Placements.Select(p => p.TeamId).Distinct().ToList();

            var transfers = await _context.PlayerTeamTransfers
                .AsNoTracking()
                .Include(tr => tr.Player)
                .Where(tr => tr.TeamId.HasValue
                             && teamIds.Contains(tr.TeamId.Value)
                             && tr.StartDate <= tournament.EndDate
                             && (tr.EndDate == null || tr.EndDate >= tournament.StartDate))
                .ToListAsync(cancellationToken);

            var grouped = tournament.Placements
                .OrderBy(p => p.Placement)
                .GroupBy(p => p.Placement)
                .ToList();

            var dto = new TournamentDiscordResultsDto
            {
                TournamentId = tournament.Id,
                TournamentDetailsRouteSegment = detailsSeg,
                TournamentName = tournament.FullName,
                GameId = tournament.GameId
            };

            foreach (var g in grouped)
            {
                var list = g.OrderBy(p => p.TeamId).ToList();
                var label = PlacementBandLabel(g.Key, list.Count);
                var tierEmoji = TierEmoji(g.Key, list.Count);

                var band = new DiscordPlacementBandDto
                {
                    Label = label,
                    TierEmoji = tierEmoji,
                    Teams = new List<DiscordPlacementTeamDto>()
                };

                foreach (var pl in list)
                {
                    var team = pl.Team;
                    if (team == null)
                        continue;

                    var teamTransfers = transfers
                        .Where(t => t.TeamId == team.Id)
                        .ToList();

                    var participants = teamTransfers
                        .Where(t => t.Status == PlayerTeamStatus.Active || t.Status == PlayerTeamStatus.Substitute)
                        .Select(t => t.Player.Nickname)
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList();

                    var es = teamTransfers
                        .Where(t => t.Status == PlayerTeamStatus.EmergencySubstitute)
                        .Select(t => t.Player.Nickname)
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList();

                    band.Teams.Add(new DiscordPlacementTeamDto
                    {
                        TeamId = team.Id,
                        Tag = team.Tag,
                        FullName = team.FullName,
                        LogoAbsoluteUrl = ToAbsoluteUrl(baseUrl, team.LogoURL),
                        TeamDetailsUrl = TeamDetailsUrlWithInformation(baseUrl, team),
                        PointsAwarded = pl.PointsAwarded,
                        OrganisationPointsAwarded = pl.OrganisationPointsAwarded,
                        ParticipantNicknames = participants,
                        EmergencySubstituteNicknames = es
                    });
                }

                dto.Bands.Add(band);
            }

            var mvpTrophy = await _context.TrophyTournaments
                .AsNoTracking()
                .Where(t => t.TournamentId == tournamentId && t.AwardType == "MVP")
                .Include(t => t.PlayerTrophies)
                .ThenInclude(pt => pt.Player)
                .FirstOrDefaultAsync(cancellationToken);

            if (mvpTrophy?.PlayerTrophies.FirstOrDefault()?.Player is { } mvpPlayer)
            {
                dto.Mvp = await BuildAwardPlayerAsync(
                    mvpPlayer.Id,
                    tournament,
                    baseUrl,
                    cancellationToken);
            }

            var evpTrophy = await _context.TrophyTournaments
                .AsNoTracking()
                .Where(t => t.TournamentId == tournamentId && t.AwardType == "EVP")
                .Include(t => t.PlayerTrophies)
                .ThenInclude(pt => pt.Player)
                .FirstOrDefaultAsync(cancellationToken);

            if (evpTrophy != null)
            {
                foreach (var pt in evpTrophy.PlayerTrophies.OrderBy(x => x.PlayerId))
                {
                    var award = await BuildAwardPlayerAsync(
                        pt.PlayerId,
                        tournament,
                        baseUrl,
                        cancellationToken);
                    if (award != null)
                        dto.Evps.Add(award);
                }
            }

            return dto;
        }

        private async Task<DiscordAwardPlayerDto?> BuildAwardPlayerAsync(
            int playerId,
            Tournament tournament,
            string baseUrl,
            CancellationToken cancellationToken)
        {
            var player = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
            if (player == null)
                return null;

            var tr = await _context.PlayerTeamTransfers
                .AsNoTracking()
                .Include(t => t.Team)
                .Where(t => t.PlayerId == playerId
                            && t.StartDate <= tournament.EndDate
                            && (t.EndDate == null || t.EndDate >= tournament.StartDate))
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            var dto = new DiscordAwardPlayerDto
            {
                PlayerId = player.Id,
                Nickname = player.Nickname,
                PlayerProfileUrl = PlayerProfileUrlWithInformation(baseUrl, player)
            };

            if (tr?.Team != null)
            {
                dto.TeamId = tr.Team.Id;
                dto.TeamName = tr.Team.FullName;
                dto.TeamTag = tr.Team.Tag;
                dto.TeamLogoAbsoluteUrl = ToAbsoluteUrl(baseUrl, tr.Team.LogoURL);
                dto.TeamDetailsUrl = TeamDetailsUrlWithInformation(baseUrl, tr.Team);
            }

            return dto;
        }

        private static string TeamDetailsUrlWithInformation(string baseUrl, Team team)
        {
            baseUrl = baseUrl.TrimEnd('/');
            return $"{baseUrl}/Teams/Details/{team.Id}?information={Uri.EscapeDataString(team.GetInformation())}";
        }

        private static string PlayerProfileUrlWithInformation(string baseUrl, Player player)
        {
            baseUrl = baseUrl.TrimEnd('/');
            return $"{baseUrl}/Players/Profile/{player.Id}?information={Uri.EscapeDataString(player.GetInformation())}";
        }

        public static string ToAbsoluteUrl(string baseUrl, string? pathOrUrl)
        {
            baseUrl = baseUrl.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(pathOrUrl))
                return baseUrl + "/uploads/teams/_default.png";

            if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return pathOrUrl;

            var path = pathOrUrl.StartsWith("~/", StringComparison.Ordinal)
                ? pathOrUrl[2..]
                : pathOrUrl.TrimStart('~');
            if (!path.StartsWith('/'))
                path = "/" + path;
            return baseUrl + path;
        }

        private static string PlacementBandLabel(int placement, int teamCount)
        {
            if (teamCount <= 1)
                return OrdinalPlacement(placement);

            var end = placement + teamCount - 1;
            return $"{OrdinalPlacement(placement)}–{OrdinalPlacement(end)}";
        }

        private static string OrdinalPlacement(int n)
        {
            if (n <= 0)
                return n.ToString();
            var s = n % 100;
            if (s is >= 11 and <= 13)
                return $"{n}th";
            return (n % 10) switch
            {
                1 => $"{n}st",
                2 => $"{n}nd",
                3 => $"{n}rd",
                _ => $"{n}th"
            };
        }

        private static string TierEmoji(int placementStart, int teamCount)
        {
            if (placementStart == 1)
                return "🏆";
            if (placementStart == 2)
                return "🥈";
            if (placementStart == 3 && teamCount == 1)
                return "🥉";
            return "🏅";
        }
    }
}
