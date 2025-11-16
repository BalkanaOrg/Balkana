namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceItMapVotingDTO
    {
        public List<string> pick { get; set; }       // usually the map that was played
        public List<FaceItMapEntityDTO> entities { get; set; }
    }
}
