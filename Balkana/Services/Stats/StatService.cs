namespace Balkana.Services.Stats
{
    using AutoMapper;
    using AutoMapper.QueryableExtensions;
    using Balkana.Data;
    using Balkana.Services.Stats.Models;

    public class StatService : IStatService
    {
        private readonly ApplicationDbContext data;
        private readonly IConfigurationProvider mapper;

        public StatService(ApplicationDbContext data, IMapper mapper)
        {
            this.data = data;
            this.mapper = mapper.ConfigurationProvider;
        }


        public StatsDetailsServiceModel Details(int id)
            => this.data
            .PlayerStatistics_CS2
            .Where(x => x.Id == id)
            .ProjectTo<StatsDetailsServiceModel>(this.mapper)
            .FirstOrDefault();

        public bool PlayerExists(int id)
            => this.data
            .Players
            .Any(c=>c.Id == id);

        public bool TeamExists(int id)
            =>this.data
            .Teams
            .Any(c=>c.Id==id);

        public IEnumerable<string> GetAllPlayers()
            => this.data.Players
            .Select(c => c.Nickname)
            .ToList();
        public IEnumerable<string> GetAllTeams()
            => this.data.Teams
            .Select(c => c.FullName)
            .ToList();

        public IEnumerable<StatsPlayerServiceModel> AllPlayers()
            =>this.data.Players
            .ProjectTo<StatsPlayerServiceModel>(this.mapper)
            .ToList();

        public IEnumerable<StatsTeamServiceModel> AllTeams()
            =>this.data.Teams
            .ProjectTo<StatsTeamServiceModel>(this.mapper)
            .ToList();
    }
}
