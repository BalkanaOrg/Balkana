namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceitMatchSummaryDto
    {
        public string match_id { get; set; }
        public string game_id { get; set; }
        public string competition_name { get; set; }
        public string competition_type { get; set; }
        public string match_type { get; set; }
        public long started_at { get; set; }
        public long finished_at { get; set; }
        public string status { get; set; }
    }
}
