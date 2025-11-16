namespace Balkana.Data.DTOs.Riot
{
    public class RiotTeamDto
    {
        public int teamId { get; set; }
        public bool win { get; set; }

        public RiotObjectivesDto objectives { get; set; }
    }
}
