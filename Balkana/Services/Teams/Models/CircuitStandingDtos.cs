using System;

namespace Balkana.Services.Teams.Models
{
    public sealed class CircuitStandingTeamDto
    {
        public int TeamId { get; init; }
        public string Tag { get; init; } = "";
        public string FullName { get; init; } = "";
        /// <summary>Relative or absolute logo URL as stored on the team.</summary>
        public string? LogoURL { get; init; }
        public int GameId { get; init; }
        public bool IsLeagueOfLegends { get; init; }
        public int TotalPoints { get; init; }

        public int? LolTeamPlacementPoints { get; init; }
        public IReadOnlyList<CircuitStandingLolRosterLine> LolRoster { get; init; } =
            Array.Empty<CircuitStandingLolRosterLine>();

        public int? CsRosterPlayerPoints { get; init; }
        public int? CsOrganisationPoints { get; init; }
        public IReadOnlyList<CircuitStandingCsPlayerLine> CsPlayers { get; init; } =
            Array.Empty<CircuitStandingCsPlayerLine>();
    }

    public sealed class CircuitStandingLolRosterLine
    {
        public string Nickname { get; init; } = "";
        public int? PositionId { get; init; }
        public string PositionName { get; init; } = "";
    }

    public sealed class CircuitStandingCsPlayerLine
    {
        public string Nickname { get; init; } = "";
        public int? PositionId { get; init; }
        public string PositionName { get; init; } = "";
        public int PointsThisYear { get; init; }
    }
}
