using AutoMapper;
using AutoMapper.QueryableExtensions;
using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Transfers;
using Balkana.Services.Teams.Models;
using Balkana.Services.Transfers.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Balkana.Services.Transfers
{
    public class TransferService : ITransferService
    {
        private readonly ApplicationDbContext data;
        private readonly IMapper mapper;
        private readonly AutoMapper.IConfigurationProvider con;

        public TransferService(ApplicationDbContext data, IMapper mapper)
        {
            this.data = data;
            this.mapper = mapper;
            this.con = mapper.ConfigurationProvider;
        }

        public TransferQueryServiceModel All(
            string game = null,
            string searchTerm = null,
            int currentPage = 1,
            int transfersPerPage = int.MaxValue,
            DateTime? asOfDate = null // NEW: filter by date (optional)
        )
        {
            var query = this.data.PlayerTeamTransfers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(game))
            {
                query = query.Where(c => c.Team.Game.FullName == game || c.Team.Game.ShortName == game);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lower = searchTerm.ToLower();
                query = query.Where(c =>
                    c.Player.Nickname.ToLower().Contains(lower) ||
                    c.Team.Tag.ToLower().Contains(lower) ||
                    c.Team.FullName.ToLower().Contains(lower) ||
                    c.Team.Game.ShortName.ToLower().Contains(lower) ||
                    c.Team.Game.FullName.ToLower().Contains(lower));
            }

            // If we're querying roster at a point in time
            if (asOfDate.HasValue)
            {
                var date = asOfDate.Value;
                query = query.Where(c =>
                    c.StartDate <= date &&
                    (c.EndDate == null || c.EndDate >= date));
            }

            var total = query.Count();

            var transfers = GetTransfers(
                query
                    .OrderByDescending(c => c.StartDate)
                    .Skip((currentPage - 1) * transfersPerPage)
                    .Take(transfersPerPage)
            );

            return new TransferQueryServiceModel
            {
                TotalTransfers = total,
                CurrentPage = currentPage,
                TransfersPerPage = transfersPerPage,
                Transfers = transfers
            };
        }

        public int Create(int playerId, int? teamId, DateTime startDate, int positionId, PlayerTeamStatus status)
        {
            // Close existing active contracts
            var current = this.data.PlayerTeamTransfers
                .Where(t => t.PlayerId == playerId && t.EndDate == null)
                .ToList();

            foreach (var c in current)
                c.EndDate = startDate;

            var transfer = new PlayerTeamTransfer
            {
                PlayerId = playerId,
                TeamId = teamId,
                StartDate = startDate,
                PositionId = positionId,
                Status = status
            };

            this.data.PlayerTeamTransfers.Add(transfer);
            this.data.SaveChanges();

            return transfer.Id;
        }

        public bool Edit(int id, int positionId, DateTime? newStartDate = null)
        {
            var transfer = this.data.PlayerTeamTransfers.Find(id);

            if (transfer == null)
                return false;

            transfer.PositionId = positionId;

            if (newStartDate.HasValue)
                transfer.StartDate = newStartDate.Value; // careful: affects history

            this.data.SaveChanges();
            return true;
        }

        // Disable hard delete
        public bool Invalidate(int id)
        {
            var transfer = this.data.PlayerTeamTransfers.Find(id);
            if (transfer == null) return false;

            transfer.EndDate = transfer.StartDate; // makes it invalid immediately
            this.data.SaveChanges();
            return true;
        }

        //public TransferDetailsServiceModel Details(int id)
        //{
        //    var transfer = this.data.PlayerTeamTransfers
        //        .FirstOrDefault(c => c.Id == id);

        //    return transfer == null ? null : this.mapper.Map<TransferDetailsServiceModel>(transfer);
        //}
        public TransferDetailsServiceModel Details(int id)
            => this.data.PlayerTeamTransfers
            .Where(c => c.Id == id)
            .ProjectTo<TransferDetailsServiceModel>(this.con)
            .FirstOrDefault();

        public IEnumerable<TransfersServiceModel> GetTransfers(IQueryable<PlayerTeamTransfer> transfers)
            => transfers.Select(t => new TransfersServiceModel
            {
                Id = t.Id,
                PlayerId = t.PlayerId,
                PlayerUsername = t.Player.Nickname,
                TeamId = t.TeamId,
                TeamFullName = t.Team != null ? t.Team.FullName : "Free Agent",
                GameId = t.Team != null ? t.Team.GameId : 0,
                GameName = t.Team != null ? t.Team.Game.FullName : "-",
                IconUrl = t.Team.Game.IconURL,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Status = t.Status,
                PositionId = t.PositionId,
                Position = t.TeamPosition != null ? t.TeamPosition.Name : "-"
            }).ToList();

        public IEnumerable<TransfersServiceModel> RosterAtDate(int teamId, DateTime date)
        {
            var query = this.data.PlayerTeamTransfers
                .Where(t =>
                    t.TeamId == teamId &&
                    t.StartDate <= date &&
                    (t.EndDate == null || t.EndDate >= date) &&
                    t.Status == PlayerTeamStatus.Active);

            return GetTransfers(query);
        }





        //all teams
        public IEnumerable<string> GetAllTeams(int gameId)
            => this.data
                .Teams
                .Where(c=>c.GameId==gameId)
                .Select(c=>c.FullName)
                .ToList();

        public IEnumerable<string> GetAllTeams()
            => this.data
                .Teams
                .Select(c=>c.FullName)
                .ToList();
        public bool TeamExists(int teamId)
            => this.data
            .Teams
            .Any(c => c.Id == teamId);

        public IEnumerable<TransferTeamsServiceModel> AllTeams()
            => this.data
            .Teams
            .ProjectTo<TransferTeamsServiceModel>(this.con)
            .ToList();
        

        //All players
        public IEnumerable<string> GetAllPlayers(int gameId)
            => this.data
                .PlayerTeamTransfers
                .Where(c=>c.Team.GameId==gameId)
                .Select(c=>c.Player.Nickname)
                .ToList();
        public IEnumerable<string> GetAllPlayers()
            => this.data
                .PlayerTeamTransfers
                .Select(c=>c.Player.Nickname)
                .ToList();
        public bool PlayerExists(int playerId)
            => this.data
            .Players
            .Any(c => c.Id == playerId);

        public IEnumerable<TransferPlayersServiceModel> AllPlayers()
            => this.data
            .Players
            .Select(a => new TransferPlayersServiceModel
            {
                Id = a.Id,
                Nickname = a.Nickname
            })
            .ToList();


        //all games
        public IEnumerable<string> GetAllGames()
            =>this.data
                .Games
                .Select(c=>c.FullName)
                .ToList();

        public IEnumerable<TeamGameServiceModel> AllGames()
            => this.data
            .Games
            .ProjectTo<TeamGameServiceModel>(this.con)
            .ToList();

        public bool GameExists(int gameId)
            =>this.data
            .Games
            .Any(c=>c.Id == gameId);


        //Team positions
        public IEnumerable<string> GetAllPositions(int gameId)
            => this.data
                .PlayerTeamTransfers
                .Where(c => c.Team.GameId == gameId)
                .Select(c => c.Player.Nickname)
                .ToList();
        public IEnumerable<string> GetAllPositions()
            => this.data
                .Positions
                .Select(c => c.Name)
                .ToList();
        public IEnumerable<TransferPositionsServiceModel> AllPositions()
            => this.data
            .Positions
            .Select(a => new TransferPositionsServiceModel
            {
                Id = a.Id,
                Name = a.Name,
                IconUrl = a.Icon,
                GameId = a.GameId
            })
            .ToList();

        public bool PositionExists(int id)
            => this.data
            .Positions
            .Any(c => c.Id == id);


        //all transfers
        public IEnumerable<int> GetAllTransfers(int id)
            => this.data
                .PlayerTeamTransfers
                .Where(c => c.Id == id)
                .Select(c => c.Id)
                .ToList();
        public IEnumerable<int> GetAllTransfers()
            => this.data
                .PlayerTeamTransfers
                .Select(c => c.Id)
                .ToList();
        public IEnumerable<TransferPositionsServiceModel> AllTransfers()
            => this.data
            .PlayerTeamTransfers
            .ProjectTo<TransferPositionsServiceModel>(this.con)
            .ToList();

        public bool TransferExists(int id)
            => this.data
            .PlayerTeamTransfers
            .Any(c => c.Id == id);


        public IEnumerable<TransferTeamsServiceModel> GetTeams(int gameId, string? search, int page, int pageSize)
        {
            var query = this.data.Teams
                .AsNoTracking()
                .Where(t => t.GameId == gameId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    t.FullName.Contains(search) ||
                    t.Tag.Contains(search));
            }

            return query
                .OrderBy(t => t.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransferTeamsServiceModel
                {
                    Id = t.Id,
                    FullName = t.FullName,
                    Tag = t.Tag,
                    LogoUrl = t.LogoURL
                })
                .ToList();
        }

        public IEnumerable<TransferPlayersServiceModel> GetPlayers(int gameId, string? search, int page, int pageSize)
        {
            // Players are linked to Games via GameProfiles
            var query = this.data.Players.AsNoTracking();
            //var query = this.data.Players
            //    .AsNoTracking()
            //    .Where(p => p.GameProfiles.Any(gp => gp.GameId == gameId));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Nickname.Contains(search) ||
                    p.FirstName.Contains(search) ||
                    p.LastName.Contains(search));
            }

            return query
                .OrderBy(p => p.Nickname)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new TransferPlayersServiceModel
                {
                    Id = p.Id,
                    Nickname = p.Nickname
                })
                .ToList();
        }

        public IEnumerable<TransferPositionsServiceModel> GetPositions(int gameId)
        {
            return this.data.Positions
                .AsNoTracking()
                .Where(tp => tp.GameId == gameId)
                .OrderBy(tp => tp.Id)
                .Select(tp => new TransferPositionsServiceModel
                {
                    Id = tp.Id,
                    Name = tp.Name,
                    IconUrl = tp.Icon
                })
                .ToList();
        }
    }
}
