namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceItRoundDto
    {
        public string match_id { get; set; }
        public string match_round { get; set; }
        public string played { get; set; }
        public RoundStats round_stats { get; set; }   // ✅ add this
        public List<FaceItTeamStatsDto> teams { get; set; }
    }
}
