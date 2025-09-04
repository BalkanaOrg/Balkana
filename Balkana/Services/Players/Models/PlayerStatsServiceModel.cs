namespace Balkana.Services.Players.Models
{
    public class PlayerStatsServiceModel
    {
        public int PlayerId { get; set; }
        public string Nickname { get; set; }

        // Which games the player has played (used for filter buttons)
        public List<string> GamesPlayed { get; set; } = new();

        // Selected game to display
        public string SelectedGame { get; set; }

        // Stats grouped by series
        public List<PlayerSeriesStatsServiceModel> SeriesStats { get; set; } = new();

    }
}
