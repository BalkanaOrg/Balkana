using AutoMapper;
using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Data.Infrastructure.Extensions;
using Balkana.Data.Infrastructure;
using Balkana.Models.Teams;
using Balkana.Services.Teams;
using Balkana.Services.Teams.Models;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using IOFile = System.IO.File;

namespace Balkana.Controllers
{
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext data;
        private readonly ITeamService teams;
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment env;

        public TeamsController(ApplicationDbContext data, ITeamService teams, IMapper mapper, IWebHostEnvironment env)
        {
            this.data = data;
            this.teams = teams;
            this.mapper = mapper;
            this.env = env;
            Console.WriteLine(">>> WebRootPath = " + env.WebRootPath);
        }

        public IActionResult Index([FromQuery] AllTeamsQueryModel query)
        {
            var queryResult = this.teams.All(
                query.Game,
                query.SearchTerm,
                query.Year,
                query.CurrentPage,
                AllTeamsQueryModel.TeamsPerPage);

            var teamGames = this.teams.GetAllGames();
            var availableYears = this.teams.GetAvailableYears();
            var absoluteNumberTeams = this.teams.AbsoluteNumberOfTeams();

            query.Games = teamGames;
            query.AvailableYears = availableYears;
            query.TotalTeams = queryResult.TotalTeams;
            query.Teams = queryResult.Teams;
            query.AbsoluteNumberOfTeams = absoluteNumberTeams;

            query.SelectedGame = query.Game;
            
            // Calculate starting rank for the partial view
            ViewBag.StartRank = (query.CurrentPage - 1) * AllTeamsQueryModel.TeamsPerPage + 1;

            return View(query);
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Add()
        {
            //if(!this.User.IsAdmin())
            //{
            //    return RedirectToAction("Index", "Teams");
            //}
            return View(new TeamFormModel
            {
                CategoriesGames = this.teams.AllGames()
            });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Add(TeamFormModel team)
        {
            Console.WriteLine("ModelState.IsValid = " + ModelState.IsValid);
            Console.WriteLine("LogoFile = " + (team.LogoFile?.FileName ?? "NULL"));

            if (!this.teams.GameExists(team.GameId))
            {
                this.ModelState.AddModelError(nameof(team.GameId), "Game doesn't exist.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Validation failed");
                team.CategoriesGames = this.teams.AllGames();
                return View(team);
            }

            string? logoPath = null;

            if (team.LogoFile != null && team.LogoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "TeamLogos");
                Directory.CreateDirectory(uploadsFolder);

                var ext = Path.GetExtension(team.LogoFile.FileName);
                var finalFileName = $"{Guid.NewGuid()}{ext}";
                var finalPath = Path.Combine(uploadsFolder, finalFileName);

                // write to temp first
                var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);

                try
                {
                    Console.WriteLine($">>> Writing upload to temp: {tempFile}");
                    await using (var tempStream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await team.LogoFile.CopyToAsync(tempStream);
                        await tempStream.FlushAsync();
                    }

                    // move to final destination atomically
                    Console.WriteLine($">>> Moving temp file to final path: {finalPath}");
                    if (System.IO.File.Exists(finalPath))
                    {
                        Console.WriteLine($">>> Final path already exists, deleting: {finalPath}");
                        System.IO.File.Delete(finalPath);
                    }
                    System.IO.File.Move(tempFile, finalPath);

                    logoPath = $"/uploads/TeamLogos/{finalFileName}";
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine("❌ IO Exception while saving file: " + ioEx);
                    try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch { }
                    ModelState.AddModelError("", "File write error: " + ioEx.Message);
                    team.CategoriesGames = this.teams.AllGames();
                    return View(team);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ General Exception while saving file: " + ex);
                    try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch { }
                    ModelState.AddModelError("", "Unexpected error while saving file.");
                    team.CategoriesGames = this.teams.AllGames();
                    return View(team);
                }
            }


            var teamId = this.teams.Create(
                team.FullName,
                team.Tag,
                logoPath ?? string.Empty,
                team.YearFounded,
                team.GameId);

            Console.WriteLine("Team created with ID = " + teamId);

            var tteam = this.teams.Details(teamId);
            string teamInformation = tteam.GetInformation();

            return RedirectToAction(nameof(Details), new { id = teamId, information = teamInformation });
        }

        public IActionResult Details(int id, string information)
        {
            var team = this.teams.Details(id);

            if(information != team.GetInformation())
            {
                return BadRequest();
            }

            team.Players = this.teams.AllPlayers(id);

            return View(team);
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult Edit(int id)
        {
            var team = this.teams.Details(id);
            if (team == null) return NotFound();

            var model = new TeamFormModel
            {
                FullName = team.FullName,
                Tag = team.Tag,
                LogoPath = team.LogoURL, // show current logo
                GameId = team.GameId,
                YearFounded = team.yearFounded,
                CategoriesGames = this.teams.AllGames()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Edit(int id, TeamFormModel model)
        {
            if (!this.teams.GameExists(model.GameId))
            {
                this.ModelState.AddModelError(nameof(model.GameId), "Game doesn't exist.");
            }

            if (!ModelState.IsValid)
            {
                model.CategoriesGames = this.teams.AllGames();
                return View(model);
            }

            string? logoPath = model.LogoPath; // keep old logo by default

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "TeamLogos");
                Directory.CreateDirectory(uploadsFolder);

                var ext = Path.GetExtension(model.LogoFile.FileName);
                var finalFileName = $"{Guid.NewGuid()}{ext}";
                var finalPath = Path.Combine(uploadsFolder, finalFileName);

                var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ext);

                try
                {
                    await using (var tempStream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await model.LogoFile.CopyToAsync(tempStream);
                        await tempStream.FlushAsync();
                    }

                    if (System.IO.File.Exists(finalPath))
                    {
                        System.IO.File.Delete(finalPath);
                    }
                    System.IO.File.Move(tempFile, finalPath);

                    logoPath = $"/uploads/TeamLogos/{finalFileName}";
                }
                catch
                {
                    if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
                    ModelState.AddModelError("", "Error saving uploaded file.");
                    model.CategoriesGames = this.teams.AllGames();
                    return View(model);
                }
            }

            // Update the team in your service
            this.teams.Update(
                id,
                model.FullName,
                model.Tag,
                logoPath ?? string.Empty,
                model.YearFounded,
                model.GameId);

            var updated = this.teams.Details(id);
            string info = updated.GetInformation();

            return RedirectToAction(nameof(Details), new { id, information = info });
        }

    }
}
