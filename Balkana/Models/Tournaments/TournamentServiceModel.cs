using Balkana.Services.Teams.Models;

namespace Balkana.Models.Tournaments
{
    public class TournamentServiceModel
    {
        public IEnumerable<TeamServiceModel> Teams { get; set; }
    }
}
