using Balkana.Services.Stats.Models;

namespace Balkana.Services.Stats
{
    public interface IStatService
    {

        StatsDetailsServiceModel Details(int id);

        bool PlayerExists(int playerId);
        bool TeamExists(int teamId);

        public IEnumerable<string> GetAllPlayers();
        public IEnumerable<string> GetAllTeams();

        public IEnumerable<StatsPlayerServiceModel> AllPlayers();
        public IEnumerable<StatsTeamServiceModel> AllTeams();

    }
}
