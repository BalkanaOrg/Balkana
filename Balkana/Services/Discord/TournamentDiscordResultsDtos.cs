namespace Balkana.Services.Discord
{
    public class TournamentDiscordResultsDto
    {
        public int TournamentId { get; set; }
        /// <summary>URL path segment for /Tournaments/Details/{segment} (ShortName, or id when missing).</summary>
        public string TournamentDetailsRouteSegment { get; set; } = "";
        public string TournamentName { get; set; } = "";
        public int GameId { get; set; }
        public List<DiscordPlacementBandDto> Bands { get; set; } = new();
        public DiscordAwardPlayerDto? Mvp { get; set; }
        public List<DiscordAwardPlayerDto> Evps { get; set; } = new();
    }

    public class DiscordPlacementBandDto
    {
        public string Label { get; set; } = "";
        public string TierEmoji { get; set; } = "";
        public List<DiscordPlacementTeamDto> Teams { get; set; } = new();
    }

    public class DiscordPlacementTeamDto
    {
        public int TeamId { get; set; }
        public string Tag { get; set; } = "";
        public string FullName { get; set; } = "";
        public string LogoAbsoluteUrl { get; set; } = "";
        public string TeamDetailsUrl { get; set; } = "";
        public int PointsAwarded { get; set; }
        public int OrganisationPointsAwarded { get; set; }
        public List<string> ParticipantNicknames { get; set; } = new();
        public List<string> EmergencySubstituteNicknames { get; set; } = new();
    }

    public class DiscordAwardPlayerDto
    {
        public int PlayerId { get; set; }
        public string Nickname { get; set; } = "";
        public string PlayerProfileUrl { get; set; } = "";
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public string? TeamTag { get; set; }
        public string? TeamLogoAbsoluteUrl { get; set; }
        public string? TeamDetailsUrl { get; set; }
    }
}
