

namespace Balkana.Services.Players
{
    using AutoMapper;
    using AutoMapper.QueryableExtensions;
    using Balkana.Data;
    using Balkana.Data.Models;
    using Balkana.Models.Players;
    using Balkana.Services.Players.Models;
    using Balkana.Services.Teams;
    using Balkana.Services.Transfers.Models;
    using Microsoft.EntityFrameworkCore;

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
        {
            var defaultPic = "https://media.istockphoto.com/id/1618846975/photo/smile-black-woman-and-hand-pointing-in-studio-for-news-deal-or-coming-soon-announcement-on.jpg?s=612x612&w=0&k=20&c=LUvvJu4sGaIry5WLXmfQV7RStbGG5hEQNo8hEFxZSGY=";

            var player = this.data.Players
        .Where(p => p.Id == id)
        .Select(p => new PlayerDetailsServiceModel
        {
            Id = p.Id,
            Nickname = p.Nickname,
            FirstName = p.FirstName,
            LastName = p.LastName,
            NationalityId = p.NationalityId,
            NationalityName = p.Nationality.Name,
            FlagUrl = p.Nationality.FlagURL,

            PictureUrl = p.PlayerPictures
                .OrderByDescending(pic => pic.dateChanged)
                .Select(pic => pic.PictureURL)
                .FirstOrDefault() ?? defaultPic,

            PlayerPictures = p.PlayerPictures
                .OrderByDescending(pic => pic.dateChanged)
                .Select(pic => new PlayerPictureServiceModel
                {
                    Id = pic.Id,
                    PictureUrl = pic.PictureURL,
                    DateChanged = pic.dateChanged
                }),

            Trophies = p.PlayerTrophies
                .Where(pt => pt.Trophy.AwardType != "MVP" && pt.Trophy.AwardType != "EVP")
                .Select(pt => new PlayerTrophyServiceModel
                {
                    Id = pt.Trophy.Id,
                    Description = pt.Trophy.Description,
                    IconURL = pt.Trophy.IconURL,
                    AwardDate = pt.Trophy.AwardDate
                }).ToList(),

            MVPs = p.PlayerTrophies
                .Where(pt => pt.Trophy.AwardType == "MVP")
                .Select(pt => new PlayerTrophyServiceModel
                {
                    Id = pt.Trophy.Id,
                    Description = pt.Trophy.Description,
                    IconURL = pt.Trophy.IconURL,
                    AwardDate = pt.Trophy.AwardDate
                }).ToList(),

            EVPs = p.PlayerTrophies
                .Where(pt => pt.Trophy.AwardType == "EVP")
                .Select(pt => new PlayerTrophyServiceModel
                {
                    Id = pt.Trophy.Id,
                    Description = pt.Trophy.Description,
                    IconURL = pt.Trophy.IconURL,
                    AwardDate = pt.Trophy.AwardDate
                }).ToList()
        })
        .FirstOrDefault();

            if (player == null) return null;

            // Fetch transfers grouped by game
            var transfers = this.data.PlayerTeamTransfers
                .Where(t => t.PlayerId == id)
                .Include(t => t.Team)
                .Include(t => t.TeamPosition)
                    .ThenInclude(tp => tp.Game)
                .OrderByDescending(t => t.StartDate)
                .Select(t => new TransferDetailsServiceModel
                {
                    Id = t.Id,
                    TeamId = t.TeamId,
                    TeamFullName = t.Team.FullName,
                    GameId = t.TeamPosition.GameId,
                    GameName = t.TeamPosition.Game.FullName,
                    PositionId = (int)t.PositionId,
                    Position = t.TeamPosition.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    Status = t.Status
                })
                .ToList();

            player.TransfersByGame = transfers
                .GroupBy(t => t.GameName)
                .ToDictionary(g => g.Key, g => g.ToList());

            return player;
        }

        public PlayerStatsServiceModel Stats(int id, string? gameFilter = null)
        {
            var player = this.data.Players
                .Include(p => p.GameProfiles)
                .FirstOrDefault(p => p.Id == id);

            if (player == null) return null;

            var playerUUIDs = player.GameProfiles.Select(g => g.UUID).ToList();

            // Find all stats across games
            var statsQuery = this.data.Set<PlayerStatistic>()
                .Include(s => s.Match).ThenInclude(m => m.Series)
                .Where(s => playerUUIDs.Contains(s.PlayerUUID));

            if (!string.IsNullOrEmpty(gameFilter))
                statsQuery = statsQuery.Where(s => s.Source == gameFilter);

            var stats = statsQuery.ToList();

            // Detect games played
            var gamesPlayed = stats.Select(s => s.Source).Distinct().ToList();

            // Default filter = game with most matches
            if (string.IsNullOrEmpty(gameFilter))
            {
                gameFilter = stats
                    .GroupBy(s => s.Source)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key;
            }

            // Filter again with final choice
            var filtered = this.data.PlayerStats
                // join with GameProfile to resolve PlayerId from PlayerUUID
                .Join(
                    this.data.GameProfiles,
                    stats => stats.PlayerUUID,
                    profile => profile.UUID,
                    (stats, profile) => new { Stats = stats, PlayerId = profile.PlayerId }
                )
                // filter by the actual player Id
                .Where(x => x.PlayerId == id)
                .Select(x => x.Stats)
                // include related navigation properties
                .Include(s => s.Match)
                    .ThenInclude(m => m.Series)
                        .ThenInclude(se => se.Tournament)
                .ToList();

            var result = new PlayerStatsServiceModel
            {
                PlayerId = player.Id,
                Nickname = player.Nickname,
                GamesPlayed = gamesPlayed,
                SelectedGame = gameFilter,
                SeriesStats = filtered
                    .GroupBy(s => s.Match.SeriesId)
                    .Select(g => new PlayerSeriesStatsServiceModel
                    {
                        SeriesId = g.Key,
                        TournamentName = g.First().Match.Series.Tournament.FullName,
                        StartedAt = g.First().Match.Series.DatePlayed,
                        Matches = g.Select(s => new PlayerMatchStatsServiceModel
                        {
                            MatchId = s.MatchId,
                            PlayedAt = s.Match.PlayedAt,
                            //Opponent = (s.Match.TeamAId == s.Match.TeamBId ? "Unknown" :
                            //            s.Match.TeamA.Players.Any(p => p.Id == id) ?
                            //                s.Match.TeamB.FullName : s.Match.TeamA.FullName),
                            IsWinner = s.IsWinner,
                            CS2Stats = s as PlayerStatistic_CS2,
                            LoLStats = s as PlayerStatistic_LoL
                        }).ToList()
                    }).ToList()
            };

            return result;
        }

        public PlayerDetailsServiceModel GetPlayerProfile(int playerId)
        {
            var player = this.data.Players
            .Where(p => p.Id == playerId)
            .Include(p => p.PlayerTrophies)                 // ✅ Include PlayerTrophies
                .ThenInclude(pt => pt.Trophy)              // ✅ Include the Trophy itself
            .Select(p => new PlayerDetailsServiceModel
            {
                Id = p.Id,
                Nickname = p.Nickname,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PictureUrl = p.PlayerPictures
                    .OrderByDescending(pic => pic.dateChanged)
                    .Select(pic => pic.PictureURL)
                    .FirstOrDefault(),

                Trophies = p.PlayerTrophies
                    .Where(pt => pt.Trophy.AwardType != "MVP" && pt.Trophy.AwardType != "EVP")
                    .Select(pt => new PlayerTrophyServiceModel
                    {
                        Id = pt.Trophy.Id,
                        Description = pt.Trophy.Description,
                        IconURL = pt.Trophy.IconURL,
                        AwardDate = pt.Trophy.AwardDate
                    }).ToList(),

                MVPs = p.PlayerTrophies
                    .Where(pt => pt.Trophy.AwardType == "MVP")
                    .Select(pt => new PlayerTrophyServiceModel
                    {
                        Id = pt.Trophy.Id,
                        Description = pt.Trophy.Description,
                        IconURL = pt.Trophy.IconURL,
                        AwardDate = pt.Trophy.AwardDate
                    }).ToList(),

                EVPs = p.PlayerTrophies
                    .Where(pt => pt.Trophy.AwardType == "EVP")
                    .Select(pt => new PlayerTrophyServiceModel
                    {
                        Id = pt.Trophy.Id,
                        Description = pt.Trophy.Description,
                        IconURL = pt.Trophy.IconURL,
                        AwardDate = pt.Trophy.AwardDate
                    }).ToList()
            })
            .FirstOrDefault();

            var test = this.data.PlayerTrophies
    .Include(pt => pt.Trophy)
    .Where(pt => pt.PlayerId == playerId)
    .ToList();

            Console.WriteLine($"Found {test.Count} trophies for player {playerId}");

            return player;
        }

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
