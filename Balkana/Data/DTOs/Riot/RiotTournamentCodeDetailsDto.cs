using System.Collections.Generic;

namespace Balkana.Data.DTOs.Riot
{
    /// <summary>
    /// Response DTO for getting tournament code details
    /// </summary>
    public class RiotTournamentCodeDetailsDto
    {
        public string code { get; set; }
        public int id { get; set; }
        public string lobbyName { get; set; }
        public string map { get; set; }
        public string metaData { get; set; }
        public List<string> participants { get; set; }
        public string password { get; set; }
        public string pickType { get; set; }
        public int providerId { get; set; }
        public string region { get; set; }
        public string spectators { get; set; }
        public int teamSize { get; set; }
        public int tournamentId { get; set; }
    }

    /// <summary>
    /// Response DTO for getting lobby events by tournament code
    /// </summary>
    public class RiotTournamentLobbyEventDto
    {
        public string summonerId { get; set; }
        public string eventType { get; set; } // e.g., "PlayerJoinedGameEvent", "PlayerQuitGameEvent", etc.
        public string timestamp { get; set; }
    }

    public class RiotTournamentLobbyEventsDto
    {
        public List<RiotTournamentLobbyEventDto> eventList { get; set; }
    }

    /// <summary>
    /// Response DTO for getting match IDs by tournament code
    /// </summary>
    public class RiotTournamentMatchIdsDto
    {
        public List<long> MatchIds { get; set; }
    }
}

