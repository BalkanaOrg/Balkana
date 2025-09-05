using Balkana.Data.Models;
using Balkana.Services.Players.Models;

namespace Balkana.Services.Teams.Models
{
    public class TeamServiceModel : ITeamModel
    {
        public int Id { get; init; }
        public string FullName { get; init; }
        public string Tag { get; init; }
        public string LogoURL { get; init; }
        public int GameId { get; init; }
        public int yearFounded { get; init; }
        public string GameName { get; init; }

        public IEnumerable<PlayerServiceModel> Players { get; set; } = new List<PlayerServiceModel>();
    }
}
