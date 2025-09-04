namespace Balkana.Services.Players.Models
{
    public class PlayerSeriesStatsServiceModel
    {
        public int SeriesId { get; set; }
        public string TournamentName { get; set; } // assume Series has Tournament
        public DateTime StartedAt { get; set; }

        public List<PlayerMatchStatsServiceModel> Matches { get; set; } = new();
    }
}
