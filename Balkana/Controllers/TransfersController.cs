using AutoMapper;
using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Transfers;
using Balkana.Services.Transfers;
using Balkana.Services.Transfers.Models;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize(Roles = "Administrator,Moderator")]
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
        [Authorize(Roles = "Administrator,Moderator")]
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


        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Edit(int id)
        {
            var transfer = this.transfers.Details(id);

            if (transfer == null)
            {
                return NotFound();
            }

            var model = new TransferFormModel
            {
                PlayerId = transfer.PlayerId,
                TeamId = transfer.TeamId,
                GameId = transfer.GameId,
                PositionId = transfer.PositionId,
                TransferDate = transfer.TransferDate,

                TransferGames = this.transfers.AllGames(),
                TransferTeams = this.transfers.AllTeams(),
                TransferPlayers = this.transfers.AllPlayers(),
                TransferPositions = this.transfers.AllPositions()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Edit(int id, TransferFormModel model)
        {
            if (!this.transfers.TransferExists(id))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.TransferGames = this.transfers.AllGames();
                model.TransferTeams = this.transfers.AllTeams();
                model.TransferPlayers = this.transfers.AllPlayers();
                model.TransferPositions = this.transfers.AllPositions();

                return View(model);
            }

            var success = this.transfers.Edit(
                id,
                model.PlayerId,
                model.TeamId,
                model.TransferDate,
                model.PositionId
            );

            if (!success)
            {
                return BadRequest();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Delete(int id)
        {
            var transfer = this.transfers.Details(id);

            if (transfer == null)
            {
                return NotFound();
            }

            return View(transfer);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Administrator")]
        public IActionResult DeleteConfirmed(int id)
        {
            var transfer = data.PlayerTeamTransfers.Find(id);

            if (transfer == null)
            {
                return NotFound();
            }

            data.PlayerTeamTransfers.Remove(transfer);
            data.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}
