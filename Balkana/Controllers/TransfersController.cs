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
                AllTransfersQueryModel.TransfersPerPage,
                query.AsOfDate // NEW filter param in query model
            );

            query.Transfers = queryResult.Transfers;
            query.TotalTransfers = queryResult.TotalTransfers;
            query.Games = this.transfers.GetAllGames();
            query.Players = this.transfers.GetAllPlayers();
            query.Teams = this.transfers.GetAllTeams();
            query.Positions = this.transfers.GetAllPositions();

            return View(query);
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Add()
        {
            return View(new TransferFormModel
            {
                TransferGames = this.transfers.AllGames()
            });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Add([FromForm] TransferFormModel model)
        {
            if (!this.transfers.PlayerExists(model.PlayerId))
                ModelState.AddModelError(nameof(model.PlayerId), "Player doesn't exist.");

            if (model.TeamId.HasValue && !this.transfers.TeamExists(model.TeamId.Value))
                ModelState.AddModelError(nameof(model.TeamId), "Team doesn't exist.");

            if (!this.transfers.GameExists(model.GameId))
                ModelState.AddModelError(nameof(model.GameId), "Game doesn't exist.");

            if (!this.transfers.PositionExists(model.PositionId))
                ModelState.AddModelError(nameof(model.PositionId), "Position doesn't exist.");

            if (!ModelState.IsValid)
            {
                model.TransferGames = this.transfers.AllGames();
                model.TransferTeams = this.transfers.AllTeams();
                model.TransferPlayers = this.transfers.AllPlayers();
                model.TransferPositions = this.transfers.AllPositions();
                return View(model);
            }

            var transferId = this.transfers.Create(
                model.PlayerId,
                model.TeamId,
                model.StartDate,
                model.PositionId,
                model.Status
            );
            return RedirectToAction("Index");
            //return RedirectToAction(nameof(Details), new { id = transferId });
        }

        // Replace Delete with Invalidate
        [Authorize(Roles = "Administrator")]
        public IActionResult Invalidate(int id)
        {
            var success = this.transfers.Invalidate(id);
            if (!success)
                return NotFound();

            return RedirectToAction(nameof(Index));
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
                StartDate = transfer.StartDate,
                EndDate = transfer.EndDate,
                Status = transfer.Status,

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
                model.PositionId,
                model.StartDate
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

        [HttpGet]
        public IActionResult Load([FromQuery] AllTransfersQueryModel query)
        {
            var queryResult = this.transfers.All(
                query.Game,
                query.SearchTerm,
                query.CurrentPage,
                AllTransfersQueryModel.TransfersPerPage,
                query.AsOfDate
            );

            return PartialView("_TransfersPartial", queryResult.Transfers);
        }

        [HttpGet]
        public IActionResult GetTeams(int gameId, string? search = null, int page = 1, int pageSize = 20)
        {
            var result = this.transfers.GetTeams(gameId, search, page, pageSize);
            return Json(result);
        }

        [HttpGet]
        public IActionResult GetPlayers(int gameId, string? search = null, int page = 1, int pageSize = 20)
        {
            var result = this.transfers.GetPlayers(gameId, search, page, pageSize);
            return Json(result);
        }

        [HttpGet]
        public IActionResult GetPositions(int gameId)
        {
            var result = this.transfers.GetPositions(gameId);
            return Json(result);
        }
    }
}
