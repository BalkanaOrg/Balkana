using Balkana.Services.Stats.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Balkana.Services.Matches.Models
{
    public class MatchStatisticViewModel
    {
        public int MatchId { get; set; }
        public string MatchName { get; set; }
        public List<PlayerStatisticModel> PlayerStatistics { get; set; }

        public List<SelectListItem> AvailablePlayers { get; set; } = new();

        public List<int> SelectedPlayerIds { get; set; } = new();

        public string BulkStats { get; set; }
    }
}
