namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceItTeamStatsDto
    {
        public string team_id { get; set; }
        public List<FaceItPlayerStatsDto> players { get; set; }
    }
}
