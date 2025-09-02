namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceItVotingDTO
    {
        public FaceItMapVotingDTO map { get; set; }
        public List<string> voted_entity_types { get; set; }
    }
}
