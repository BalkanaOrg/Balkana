namespace Balkana.Data.Models
{
    public enum PlayerTeamStatus
    {
        Active,    // Playing normally in the roster
        Benched,   // On team but not active
        Retired,   // Career ended
        FreeAgent,  // Not tied to any team
        Substitute,
        EmergencySubstitute
    }
}
