using AutoMapper;
using AutoMapper.QueryableExtensions;
using System.Collections.Generic;
using Balkana.Data;
using Balkana.Models.Transfers;
using Balkana.Services.Transfers.Models;
using Balkana.Data.Models;
using Balkana.Services.Teams.Models;

namespace Balkana.Services.Transfers
{
    public class TransferService : ITransferService
    {
        private readonly ApplicationDbContext data;
        private readonly AutoMapper.IConfigurationProvider mapper;

        public TransferService(ApplicationDbContext data, IMapper mapper)
        {
            this.data = data;
            this.mapper = mapper.ConfigurationProvider;
        }

        public TransferQueryServiceModel All(
            string game = null,
            string searchTerm = null,
            int currentPage = 1,
            int transfersPerPage = int.MaxValue
            )
        {
            var transferQuery = this.data.PlayerTeamTransfers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(game))
            {
                transferQuery = transferQuery.Where(c => (c.Team.Game.FullName == game) || (c.Team.Game.ShortName == game));
            }
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                transferQuery = transferQuery.Where(c =>
                    (c.Player.Nickname).ToLower().Contains(searchTerm.ToLower()) ||
                    (c.Team.Tag).ToLower().Contains(searchTerm.ToLower()) ||
                    (c.Team.FullName).ToLower().Contains(searchTerm.ToLower()) ||
                    (c.Team.Game.ShortName).ToLower().Contains(searchTerm.ToLower()) ||
                    (c.Team.Game.FullName).ToLower().Contains(searchTerm.ToLower()));
            }

            var totalTransfers = transferQuery.Count();

            var transfers = GetTransfers(transferQuery.Skip((currentPage - 1) * transfersPerPage).Take(transfersPerPage));

            return new TransferQueryServiceModel
            {
                TotalTransfers = totalTransfers,
                CurrentPage = currentPage,
                TransfersPerPage = transfersPerPage,
                Transfers = transfers
            };

        }

        public int Create(int playerId, int teamId, DateTime date, int positionId)
        {
            var transferData = new PlayerTeamTransfer
            {
                PlayerId = playerId,
                TeamId = teamId,
                TransferDate = date,
                PositionId = positionId
            };

            this.data.PlayerTeamTransfers.Add(transferData);
            this.data.SaveChanges();

            return transferData.Id;
        }

        public bool Edit(int id, int playerId, int teamId, DateTime date, int positionId)
        {
            var transferData = this.data.PlayerTeamTransfers.Find(id);

            if (transferData == null)
            {
                return false;
            }

            transferData.PlayerId = playerId;
            transferData.TeamId = teamId;
            transferData.TransferDate = date;
            transferData.PositionId = positionId;

            this.data.SaveChanges();

            return true;
        }

        public TransferDetailsServiceModel Details(int id)
                => this.data
                .PlayerTeamTransfers
                .Where(c => c.Id == id)
                .ProjectTo<TransferDetailsServiceModel>(this.mapper)
                .FirstOrDefault();

        public IEnumerable<TransfersServiceModel> GetTransfers(IQueryable<PlayerTeamTransfer> transfers)
            => transfers
                .ProjectTo<TransfersServiceModel>(this.mapper)
                .ToList();

        public bool TeamExists(int teamId)
            => this.data
            .Teams
            .Any(c => c.Id == teamId);

        public IEnumerable<TransferTeamsServiceModel> AllTeams(int gameId)
            => this.data
            .Teams
            .Where(c => c.GameId == gameId)
            .ProjectTo<TransferTeamsServiceModel>(this.mapper)
            .ToList();

        public bool PlayerExists(int playerId)
            => this.data
            .Players
            .Any(c => c.Id == playerId);

        public IEnumerable<TransferPlayersServiceModel> AllPlayers(int gameId)
            => this.data
            .PlayerTeamTransfers
            .Where(c => c.Team.GameId == gameId)
            .Select(a => new TransferPlayersServiceModel
            {
                Id = a.Player.Id,
                Nickname = a.Player.Nickname
            })
            .ProjectTo<TransferPlayersServiceModel>(this.mapper)
            .ToList();

        public bool GameExists(int gameId)
            =>this.data
            .Games
            .Any(c=>c.Id == gameId);

        public IEnumerable<TeamGameServiceModel> AllGames()
            => this.data
            .Games
            .ProjectTo<TeamGameServiceModel>(this.mapper)
            .ToList();

        public IEnumerable<string> GetAllTeams(int gameId)
            => this.data
                .Teams
                .Where(c=>c.GameId==gameId)
                .Select(c=>c.FullName)
                .ToList();
        public IEnumerable<string> GetAllPlayers(int gameId)
            => this.data
                .PlayerTeamTransfers
                .Where(c=>c.Team.GameId==gameId)
                .Select(c=>c.Player.Nickname)
                .ToList();
        public IEnumerable<string> GetAllGames()
            =>this.data
                .Games
                .Select(c=>c.FullName)
                .ToList();

    }
}
