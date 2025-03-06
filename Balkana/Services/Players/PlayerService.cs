

namespace Balkana.Services.Players
{
    using AutoMapper;
    using AutoMapper.QueryableExtensions;
    using Balkana.Data;
    using Balkana.Data.Models;
    using Balkana.Models.Players;
    using Balkana.Services.Players.Models;
    using Balkana.Services.Teams;

    public class PlayerService : IPlayerService
    {
        private readonly ApplicationDbContext data;
        private readonly IConfigurationProvider mapper;

        public PlayerService(ApplicationDbContext data, IMapper mapper)
        {
            this.mapper = mapper.ConfigurationProvider;
            this.data = data;
        }

        public PlayerQueryServiceModel All(
            string searchTerm = null,
            int currentPage = 1,
            int playersPerPage = int.MaxValue
        )
        {
            var playersQuery = this.data.Players.AsQueryable();

            if(!string.IsNullOrWhiteSpace(searchTerm))
            {
                playersQuery = playersQuery.Where(c=>(c.Nickname).ToLower().Contains(searchTerm.ToLower()));
            }

            var totalPlayers = playersQuery.Count();

            var players = GetPlayers(playersQuery.Skip((currentPage - 1) * playersPerPage).Take(playersPerPage));

            return new PlayerQueryServiceModel
            {
                TotalPlayers = totalPlayers,
                CurrentPage = currentPage,
                PlayersPerPage = playersPerPage,
                Players = players
            };
        }

        public PlayerDetailsServiceModel Profile(int id)
            => this.data
            .Players
            .Where(c => c.Id == id)
            .ProjectTo<PlayerDetailsServiceModel>(this.mapper)
            .FirstOrDefault();

        public int Create(string nickname, string firstname, string lastname, int nationalityid)
        {
            var playerData = new Player
            {
                Nickname = nickname,
                FirstName = firstname,
                LastName = lastname,
                NationalityId = nationalityid
            };
            this.data.Players.Add(playerData);
            this.data.SaveChanges();

            return playerData.Id;
        }

        public bool Edit(
            int id,
            string nickname,
            string firstname,
            string lastname,
            int nationalityId)
        {
            var playerData = this.data.Players.Find(id);
            if(playerData== null)
            {
                return false;
            }

            playerData.Nickname = nickname;
            playerData.FirstName = firstname;
            playerData.LastName = lastname;
            playerData.NationalityId = nationalityId;

            this.data.SaveChanges();

            return true;
        }

        public bool PictureExists(int id)
            => this.data
                .Pictures
                .Any(c=>c.Id == id);

        public IEnumerable<PlayerPictureServiceModel> AllPlayerPictures(int playerId)
            => this.data
                .Pictures
                .Where(c => c.PlayerId == playerId)
                .ProjectTo<PlayerPictureServiceModel>(this.mapper)
                .ToList();

        public IEnumerable<PlayerNationalityServiceModel> GetNationalities()
            => this.data
            .Nationalities
            .Select(c => new PlayerNationalityServiceModel
            {
                Id = c.Id,
                Name = c.Name,
                PictureUrl = c.FlagURL
            })
            .ToList();

        private IEnumerable<PlayerServiceModel> GetPlayers(IQueryable<Player> playerQuery)
            => playerQuery
            .ProjectTo<PlayerServiceModel>(this.mapper)
            .ToList();
    }
}
