namespace Balkana.Data.DTOs.Riot
{
    /// <summary>
    /// Request DTO for creating a tournament
    /// </summary>
    public class RiotTournamentRegistrationDto
    {
        public int providerId { get; set; }
        public string name { get; set; } // optional tournament name
    }

    /// <summary>
    /// Response DTO containing tournament ID
    /// </summary>
    public class RiotTournamentRegistrationResponseDto
    {
        public int TournamentId { get; set; }
    }
}

