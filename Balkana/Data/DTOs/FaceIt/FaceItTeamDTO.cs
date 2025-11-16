namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceItTeamDTO
    {
        public string faction_id { get; set; }
        public string nickname { get; set; }
        public List<FaceItPlayerDTO> roster { get; set; }
    }
}
