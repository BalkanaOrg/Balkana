using System.Collections.Generic;

namespace Balkana.Data.DTOs.Riot
{
    /// <summary>
    /// Request DTO for generating tournament codes
    /// </summary>
    public class RiotTournamentCodeRequestDto
    {
        /// <summary>
        /// Optional list of encrypted summonerIds in order to validate players eligible to join the lobby
        /// Max 10 summoner IDs. If not specified, matches can be joined by any summoner
        /// </summary>
        public List<string> allowedSummonerIds { get; set; } = new List<string>();

        /// <summary>
        /// Optional metadata string that can be used to retrieve tournament code details
        /// Max length 1000 characters
        /// </summary>
        public string metadata { get; set; } = "";

        /// <summary>
        /// Team size for the tournament code's game (1-5)
        /// </summary>
        public int teamSize { get; set; } = 5;

        /// <summary>
        /// Pick type for the tournament code's game
        /// Values: BLIND_PICK, DRAFT_MODE, ALL_RANDOM, TOURNAMENT_DRAFT
        /// </summary>
        public string pickType { get; set; } = "TOURNAMENT_DRAFT";

        /// <summary>
        /// Map type for the tournament code's game
        /// Values: SUMMONERS_RIFT, TWISTED_TREELINE, HOWLING_ABYSS
        /// </summary>
        public string mapType { get; set; } = "SUMMONERS_RIFT";

        /// <summary>
        /// Spectator type for the tournament code's game
        /// Values: NONE, LOBBYONLY, ALL
        /// </summary>
        public string spectatorType { get; set; } = "ALL";
    }

    /// <summary>
    /// Response DTO containing generated tournament codes
    /// Riot returns an array of code strings
    /// </summary>
    public class RiotTournamentCodeResponseDto
    {
        public List<string> Codes { get; set; }
    }
}

