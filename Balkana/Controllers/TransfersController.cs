using AutoMapper;
using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Transfers;
using Balkana.Services.Transfers;
using Balkana.Services.Transfers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Controllers
{
    public class TransfersController : Controller
    {
        private readonly ApplicationDbContext data;
        private readonly ITransferService transfers;
        private readonly IMapper mapper;
        private readonly ILogger<TransfersController> _logger;

        public TransfersController(ApplicationDbContext data, ITransferService transfers, IMapper mapper, ILogger<TransfersController> logger)
        {
            this.data = data;
            this.transfers = transfers;
            this.mapper = mapper;
            this._logger = logger;
        }

        public IActionResult Index([FromQuery] AllTransfersQueryModel query)
        {
            var queryResult = this.transfers.All(
                query.Game,
                query.SearchTerm,
                query.CurrentPage,
                AllTransfersQueryModel.TransfersPerPage
                );

            var transferPositions = this.transfers.GetAllPositions();
            var transferPlayers = this.transfers.GetAllPlayers();
            var transferTeams = this.transfers.GetAllTeams();
            var transferGames = this.transfers.GetAllGames();
            var transfers = this.transfers.GetAllTransfers();

            query.Transfers = queryResult.Transfers;
            query.Games = transferGames;
            query.Players = transferPlayers;
            query.Teams = transferTeams;
            query.Positions = transferPositions;
            query.TotalTransfers = queryResult.TotalTransfers;
            return View(query);
        }

        public IActionResult Add()
        {
            return View(new TransferFormModel
            {
                TransferGames = this.transfers.AllGames(),
                TransferPositions = this.transfers.AllPositions(),
                TransferPlayers = this.transfers.AllPlayers(),
                TransferTeams = this.transfers.AllTeams()
            });
        }

        [HttpPost]
        public IActionResult Add([FromForm] TransferFormModel model)
        {
            Console.WriteLine("Received TransferFormModel:");
            Console.WriteLine($"PlayerId: {model.PlayerId}");
            Console.WriteLine($"TeamId: {model.TeamId}");
            Console.WriteLine($"GameId: {model.GameId}");
            Console.WriteLine($"PositionId: {model.PositionId}");
            Console.WriteLine($"TransferDate: {model.TransferDate}");

            if (!this.transfers.PlayerExists(model.PlayerId))
            {
                this.ModelState.AddModelError(nameof(model.PlayerId), "Player doesn't exist.");
            }
            if(!this.transfers.TeamExists(model.TeamId))
            {
                this.ModelState.AddModelError(nameof(model.TeamId), "Team doesn't exist.");
            }
            if(!this.transfers.GameExists(model.GameId))
            {
                this.ModelState.AddModelError(nameof(model.GameId), "Game doesn't exist.");
            }
            if(!this.transfers.PositionExists(model.PositionId))
            {
                this.ModelState.AddModelError(nameof(model.PositionId), "Position doesn't exist.");
            }

            if (!ModelState.IsValid)
            {
                model.TransferGames = this.transfers.AllGames();
                model.TransferTeams = this.transfers.AllTeams();
                model.TransferPlayers = this.transfers.AllPlayers();
                model.TransferPositions = this.transfers.AllPositions();
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        _logger.LogError($"❌ Error in {entry.Key}: {error.ErrorMessage}");
                    }
                }

                return View(model);

                

            }
            var transferId = this.transfers.Create(
                model.PlayerId,
                model.TeamId,
                model.TransferDate,
                model.PositionId
                );

            return RedirectToAction(nameof(Details), new { id = transferId });
        }

        public IActionResult Details(int id)
        {
            var transfer = this.transfers.Details(id);
            transfer.Players = this.transfers.AllPlayers();
            transfer.Teams = this.transfers.AllTeams();
            transfer.Games = this.transfers.AllGames();
            return View(transfer);
        }

    }
}
