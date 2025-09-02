namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceItPlayerStatsDto
    {
        public string player_id { get; set; }
        public string nickname { get; set; }
        public Dictionary<string, string> player_stats { get; set; }
    }
}
