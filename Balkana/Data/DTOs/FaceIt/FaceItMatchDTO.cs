namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceItMatchDto
    {
        public string match_id { get; set; }
        public string game { get; set; }
        public string region { get; set; }
        public string status { get; set; }
        public long started_at { get; set; }
        public long finished_at { get; set; }
        public string competition_id { get; set; }
        public string competition_name { get; set; }
        public string competition_type { get; set; }

        public FaceItMapDTO map { get; set; } // <-- change from string to object

        public Dictionary<string, FaceItTeamDTO> teams { get; set; }
        public FaceItResultsDTO results { get; set; }
        public FaceItVotingDTO voting { get; set; }
    }
}
