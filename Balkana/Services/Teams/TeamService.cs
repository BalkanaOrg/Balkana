namespace Balkana.Services.Teams
{
    using AutoMapper;
    using Balkana.Data;
    using System.Collections.Generic;
    using System.Linq;
    using AutoMapper.QueryableExtensions;
    using Balkana.Services.Teams.Models;
    using Balkana.Models.Teams;
    using Balkana.Data.Models;
    using Balkana.Services.Players.Models;

    public class TeamService : ITeamService
    {
        private readonly ApplicationDbContext data;
        private readonly IConfigurationProvider mapper;

        public TeamService(ApplicationDbContext data, IMapper mapper)
        {
            this.data = data;
            this.mapper = mapper.ConfigurationProvider;
        }

        public TeamQueryServiceModel All(
            string game = "Counter-Strike",
            string searchTerm = null,
            int currentPage = 1,
            int teamsPerPage = int.MaxValue
        )
        {
            var teamsQuery = this.data.Teams.AsQueryable();

            if(!string.IsNullOrWhiteSpace(game))
            {
                teamsQuery = teamsQuery.Where(c=>c.Game.FullName==game);
            }
            if(!string.IsNullOrWhiteSpace(searchTerm))
            {
                teamsQuery = teamsQuery.Where(c => (c.FullName + " " + c.Tag).ToLower().Contains(searchTerm.ToLower()));
            }
            var totalTeams = teamsQuery.Count();

            var teams = GetTeams(teamsQuery.Skip((currentPage - 1) * teamsPerPage).Take(teamsPerPage));

            return new TeamQueryServiceModel
            {
                TotalTeams = totalTeams,
                CurrentPage = currentPage,
                TeamsPerPage = teamsPerPage,
                Teams = teams
            };
        }
        public TeamDetailsServiceModel Details(int id)
            => this.data
                .Teams
                .Where(c => c.Id == id)
                .ProjectTo<TeamDetailsServiceModel>(this.mapper)
                .FirstOrDefault();

        public int Create(string fullname, string tag, string logoURL, int yearFounded, int gameId)
        {
            var teamData = new Team
            {
                FullName = fullname,
                Tag = tag,
                LogoURL = logoURL,
                yearFounded = yearFounded,
                GameId = gameId
            };
            this.data.Teams.Add(teamData);
            this.data.SaveChanges();

            return teamData.Id;
        }
        public bool Edit(
            int id,
            string fullname,
            string tag,
            string logo,
            int yearFounded,
            int gameId)
        {
            var teamData = this.data.Teams.Find(id);

            if(teamData == null)
            {
                return false;
            }
            teamData.FullName = fullname;
            teamData.Tag = tag;
            teamData.LogoURL = logo;
            teamData.yearFounded = yearFounded;
            teamData.GameId = gameId;

            this.data.SaveChanges();
            return true;
        }

        public IEnumerable<string> GetAllGames() 
            => this.data
                .Games
                .Select(c=>c.FullName)
                .ToList();

        public IEnumerable<TeamGameServiceModel> AllGames()
            => this.data
                .Games
                .ProjectTo<TeamGameServiceModel>(this.mapper)
                .ToList();

        public bool GameExists(int gameId)
            => this.data
                .Games
                .Any(c=>c.Id == gameId);

        //THIS NEEDS TO BE REWORKED ASAP (Показва всички играчи, независимо дали са всео ще в отбора или не)
        //public IEnumerable<TeamStaffServiceModel> AllPlayers(int teamId)
        //{
        //    var latestTransfers = this.data.PlayerTeamTransfers
        //        .Where(ptt => ptt.TeamId == teamId &&
        //            ptt.TransferDate == this.data.PlayerTeamTransfers
        //                .Where(inner => inner.PlayerId == ptt.PlayerId)
        //                .Max(inner => inner.TransferDate)) // only latest transfer per player
        //        .ProjectTo<TeamStaffServiceModel>(this.mapper)
        //        .ToList();

        //    return latestTransfers;
        //}

        public IEnumerable<TeamStaffServiceModel> AllPlayers(int teamId)
        {
            var defaultPic = "https://media.istockphoto.com/id/1618846975/photo/smile-black-woman-and-hand-pointing-in-studio-for-news-deal-or-coming-soon-announcement-on.jpg?s=612x612&w=0&k=20&c=LUvvJu4sGaIry5WLXmfQV7RStbGG5hEQNo8hEFxZSGY=";

            var latestTransfers = this.data.PlayerTeamTransfers
                .Where(ptt => ptt.TeamId == teamId &&
                    ptt.TransferDate == this.data.PlayerTeamTransfers
                        .Where(inner => inner.PlayerId == ptt.PlayerId)
                        .Max(inner => inner.TransferDate))
                .Select(ptt => new TeamStaffServiceModel
                {
                    Id = ptt.Player.Id,
                    Nickname = ptt.Player.Nickname,
                    FirstName = ptt.Player.FirstName,
                    LastName = ptt.Player.LastName,
                    PositionId = ptt.PositionId,

                    // 👇 Take latest PlayerPicture OR default
                    PictureUrl = ptt.Player.PlayerPictures
                        .OrderByDescending(pic => pic.dateChanged)
                        .Select(pic => pic.PictureURL)
                        .FirstOrDefault() ?? defaultPic
                })
                .ToList();

            return latestTransfers;
        }

        private IEnumerable<TeamServiceModel> GetTeams(IQueryable<Team> teamQuery)
        {
            var defaultPic = "https://media.istockphoto.com/id/1618846975/photo/smile-black-woman-and-hand-pointing-in-studio-for-news-deal-or-coming-soon-announcement-on.jpg?s=612x612&w=0&k=20&c=LUvvJu4sGaIry5WLXmfQV7RStbGG5hEQNo8hEFxZSGY=";

            var teams = teamQuery
                .ProjectTo<TeamServiceModel>(this.mapper)
                .ToList();

            foreach (var team in teams)
            {
                team.Players = this.AllPlayers(team.Id)
                    .Select(p => new PlayerServiceModel   // ✅ use PlayerServiceModel
                    {
                        Id = p.Id,
                        Nickname = p.Nickname,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        PictureUrl = p.PictureUrl ?? defaultPic
                    })
                    .ToList();
            }

            return teams;
        }
    }
}
