namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceItResultsDTO
    {
        public string winner { get; set; } // 0 = team1, 1 = team2
        public Dictionary<string, int> score { get; set; } // faction_id -> score
    }
}
