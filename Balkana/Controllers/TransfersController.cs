using AutoMapper;
using Balkana.Data;
using Balkana.Data.Models;
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

        public TransfersController(ApplicationDbContext data, ITransferService transfers, IMapper mapper)
        {
            this.data = data;
            this.transfers = transfers;
            this.mapper = mapper;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public JsonResult GetTeamsByGame(int gameId)
        {
            var teams = data.PlayerTeamTransfers
                .Where(ptt => ptt.Team.GameId == gameId)
                .Select(ptt => new
                {
                    id = ptt.Team.Id,
                    nickname = ptt.Team.yearFounded
                })
                .ToList();

            return Json(teams);
        }

        public IActionResult Add()
        {
            return View(new TransferFormModel
            {
                TransferGames = this.transfers.AllGames(),
                TransferPositions = this.transfers.AllPo(),
                TransferPlayers = this.transfers.AllPlayers(),
                TransferTeams = this.transfers.AllTeams()
            });
        }

        [HttpPost]
        public IActionResult Add(TransferFormModel model)
        {
            if(!this.transfers.PlayerExists(model.PlayerId))
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

            if (ModelState.ErrorCount > 1)
            {
                model.TransferGames = this.transfers.AllGames();
                model.TransferTeams = this.transfers.AllTeams(model.GameId);
                model.TransferPlayers = this.transfers.AllPlayers(model.GameId);
                model.TransferGames = this.transfers.AllGames();

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
            return View(transfer);
        }

    }
}
