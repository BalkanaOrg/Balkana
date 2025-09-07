using Balkana.Data.Models;

namespace Balkana.Data.DTOs.Bracket
{
    public class TournamentTeamRosterViewModel
    {
        public Team Team { get; set; }
        public List<Player> Players { get; set; } = new();
    }
}
