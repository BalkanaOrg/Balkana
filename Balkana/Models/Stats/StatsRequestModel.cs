using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Stats
{
    public class StatsRequestModel
    {
        public StatsRequestType RequestType { get; set; }
        
        // For player stats
        public int? PlayerId { get; set; }
        
        // For team stats
        public int? TeamId { get; set; }
        
        // For series stats
        public int? SeriesId { get; set; }
        
        // For tournament stats
        public int? TournamentId { get; set; }
        
        // Filtering options
        public int? GameId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Provider { get; set; } // "FACEIT", "RIOT", etc.
        
        // Sorting options
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
    
    public enum StatsRequestType
    {
        Player,
        Team,
        Series,
        Tournament
    }
}
