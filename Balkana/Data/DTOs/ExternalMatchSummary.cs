namespace Balkana.Data.DTOs
{
    public class ExternalMatchSummary
    {
        public string ExternalMatchId { get; set; }
        public string Source { get; set; } // "RIOT" or "FACEIT"
        public DateTime PlayedAt { get; set; }
        public bool ExistsInDb { get; set; }
    }
}
