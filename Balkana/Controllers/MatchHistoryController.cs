using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Match;
using Balkana.Services.Matches;
using Balkana.Services.Matches.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Controllers
{
    public class MatchHistoryController : Controller
    {
        private readonly MatchHistoryService _history;
        private readonly ApplicationDbContext _db;
        private readonly Dictionary<string, IMatchImporter> _importers;

        public MatchHistoryController(MatchHistoryService history, ApplicationDbContext db, Dictionary<string, IMatchImporter> importers)
        {
            _history = history;
            _db = db;
            _importers = importers;
        }

        public async Task<IActionResult> Index(string source, string profileId, int? clubId)
        {
            if (source == "FACEIT")
            {
                if (!clubId.HasValue)
                {
                    // no club selected -> show club selection page
                    var clubs = await _db.FaceitClubs
                        .Select(c => new SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = c.Name
                        })
                        .ToListAsync();

                    ViewBag.Source = source;
                    return View("SelectClub", clubs);
                }

                // get the hub id from DB
                var club = await _db.FaceitClubs.FindAsync(clubId.Value);
                if (club == null) return NotFound("Invalid club");

                // tell importer which club to use
                var importer = (FaceitMatchImporter)_importers["FACEIT"];
                importer.SetHubId(club.FaceitId);

                var matches = await importer.GetMatchHistoryAsync(profileId);
                ViewBag.Source = source;
                return View(matches);
            }

            // RIOT flow stays the same
            var riotMatches = await _history.GetHistoryAsync(source, profileId);
            ViewBag.Source = source;
            return View(riotMatches);
        }

        [HttpGet]
        public IActionResult Import(string source, string profileId, string matchId)
        {
            var vm = new MatchImportViewModel
            {
                Source = source,
                ProfileId = profileId,
                MatchId = matchId,
                Tournaments = _db.Tournaments
                    .Include(t => t.Game)
                    .Where(t => (source == "RIOT" && t.Game.ShortName == "LoL") ||
                                (source == "FACEIT" && t.Game.ShortName == "CS2"))
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.FullName
                    })
                    .ToList()
            };

            if (source == "FACEIT")
            {
                vm.Clubs = _db.FaceitClubs
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToList();
            }

            return View(vm);
        }

        [HttpGet]
        public IActionResult GetSeries(int tournamentId)
        {
            var series = _db.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Where(s => s.TournamentId == tournamentId)
                .Select(s => new
                {
                    id = s.Id,
                    text = s.TeamA.FullName + " vs " + s.TeamB.FullName + " (" + s.DatePlayed.ToShortDateString() + ") " + s.Name
                })
                .ToList();

            return Json(series);
        }

        [HttpPost]
        public async Task<IActionResult> ImportMatch(MatchImportViewModel model)
        {
            var series = await _db.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .FirstOrDefaultAsync(s => s.Id == model.SelectedSeriesId);
            
            if (series == null)
            {
                ModelState.AddModelError("", "Invalid series selected.");
                return View("Import", model);
            }

            var importer = _importers[model.Source];

            // If FaceIt, pass the chosen ClubId
            if (model.Source == "FACEIT" && model.SelectedClubId.HasValue)
            {
                var club = await _db.FaceitClubs.FindAsync(model.SelectedClubId.Value);
                if (club == null)
                {
                    ModelState.AddModelError("", "Invalid FaceIt Club selected.");
                    return View("Import", model);
                }

                // 👉 update the importer to accept dynamic HubId instead of config
                ((FaceitMatchImporter)importer).SetHubId(club.FaceitId);
            }

            var matches = await importer.ImportMatchAsync(model.MatchId, _db);

            if (matches == null || !matches.Any())
            {
                ModelState.AddModelError("", "Failed to import match data.");
                return View("Import", model);
            }

            // Add matches to series
            foreach (var match in matches)
            {
                match.SeriesId = model.SelectedSeriesId;
                _db.Matches.Add(match);
            }

            await _db.SaveChangesAsync();

            // Determine series winner and update series
            await UpdateSeriesWinner(series, matches);

            // Advance winner to next series
            await AdvanceWinnerToNextSeries(series);

            return RedirectToAction("Details", "Series", new { id = model.SelectedSeriesId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> ForfeitSeries(int seriesId, int forfeitingTeamId)
        {
            var series = await _db.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Include(s => s.Matches)
                .Include(s => s.Tournament)
                .FirstOrDefaultAsync(s => s.Id == seriesId);

            if (series == null)
            {
                TempData["Error"] = "Series not found.";
                return RedirectToAction("Index", "Tournaments");
            }

            if (series.isFinished)
            {
                TempData["Error"] = "Series is already completed.";
                return RedirectToAction("Details", "Tournaments", new { id = series.TournamentId });
            }

            // Determine the winning team (the one that didn't forfeit)
            Team winningTeam = null;
            if (forfeitingTeamId == series.TeamAId)
            {
                winningTeam = series.TeamB;
            }
            else if (forfeitingTeamId == series.TeamBId)
            {
                winningTeam = series.TeamA;
            }
            else
            {
                TempData["Error"] = "Invalid team specified for forfeit.";
                return RedirectToAction("Details", "Tournaments", new { id = series.TournamentId });
            }

            if (winningTeam == null)
            {
                TempData["Error"] = "Cannot determine winning team for forfeit.";
                return RedirectToAction("Details", "Tournaments", new { id = series.TournamentId });
            }

            // Mark series as finished and set winner
            series.isFinished = true;
            series.WinnerTeamId = winningTeam.Id;
            series.WinnerTeam = winningTeam;

            // Create a forfeit match record for tracking
            var forfeitMatch = new MatchCS
            {
                ExternalMatchId = $"FORFEIT-{seriesId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Source = "FORFEIT",
                PlayedAt = DateTime.UtcNow,
                IsCompleted = true,
                CompetitionType = "Forfeit",
                MapId = null, // No map for forfeit matches
                TeamA = series.TeamA,
                TeamAId = series.TeamAId,
                TeamASourceSlot = "TeamA",
                TeamB = series.TeamB,
                TeamBId = series.TeamBId,
                TeamBSourceSlot = "TeamB",
                WinnerTeam = winningTeam,
                WinnerTeamId = winningTeam.Id,
                SeriesId = seriesId,
                PlayerStats = new List<PlayerStatistic>()
            };

            _db.Matches.Add(forfeitMatch);
            await _db.SaveChangesAsync();

            // Advance winner to next series
            await AdvanceWinnerToNextSeries(series);

            TempData["Success"] = $"{series.TeamA?.FullName} vs {series.TeamB?.FullName} - {winningTeam.FullName} wins by forfeit.";
            return RedirectToAction("Details", "Tournaments", new { id = series.TournamentId });
        }

        private async Task UpdateSeriesWinner(Series series, List<Match> matches)
        {
            if (!matches.Any()) return;

            // Determine best-of format based on number of matches
            int bestOf = matches.Count;
            int winsNeeded = (bestOf / 2) + 1;

            // Count wins for each team
            int teamAWins = 0;
            int teamBWins = 0;

            foreach (var match in matches)
            {
                if (match.IsCompleted)
                {
                    // Determine winner based on match results
                    var winner = DetermineMatchWinner(match);
                    Console.WriteLine($"🎯 Match {match.Id} winner: {winner?.FullName ?? "null"}");
                    Console.WriteLine($"🎯 Series TeamA: {series.TeamA?.FullName ?? "null"}, Series TeamB: {series.TeamB?.FullName ?? "null"}");
                    
                    if (winner == series.TeamA)
                    {
                        teamAWins++;
                        Console.WriteLine($"✅ TeamA wins! Total: {teamAWins}");
                    }
                    else if (winner == series.TeamB)
                    {
                        teamBWins++;
                        Console.WriteLine($"✅ TeamB wins! Total: {teamBWins}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Winner doesn't match either series team!");
                    }
                }
            }

            Console.WriteLine($"📊 Series {series.Id} - TeamA wins: {teamAWins}, TeamB wins: {teamBWins}, Wins needed: {winsNeeded}");

            // Update series completion status
            if (teamAWins >= winsNeeded || teamBWins >= winsNeeded)
            {
                series.isFinished = true;
                
                // Set the series winner
                if (teamAWins >= winsNeeded)
                {
                    series.WinnerTeamId = series.TeamAId;
                    series.WinnerTeam = series.TeamA;
                    Console.WriteLine($"✅ Series {series.Id} completed! Winner: {series.TeamA?.FullName}");
                }
                else if (teamBWins >= winsNeeded)
                {
                    series.WinnerTeamId = series.TeamBId;
                    series.WinnerTeam = series.TeamB;
                    Console.WriteLine($"✅ Series {series.Id} completed! Winner: {series.TeamB?.FullName}");
                }
            }
            else
            {
                Console.WriteLine($"⏳ Series {series.Id} not yet complete.");
            }

            await _db.SaveChangesAsync();
        }

        private Team DetermineMatchWinner(Match match)
        {
            // Use the WinnerTeam directly from the match
            if (match.WinnerTeam != null)
            {
                return match.WinnerTeam;
            }

            // Fallback: determine winner from player statistics if WinnerTeam is not set
            if (match is MatchCS csMatch)
            {
                var teamAStats = csMatch.PlayerStats
                    .Where(ps => ps.Team == csMatch.TeamASourceSlot)
                    .OfType<PlayerStatistic_CS2>()
                    .ToList();
                var teamBStats = csMatch.PlayerStats
                    .Where(ps => ps.Team == csMatch.TeamBSourceSlot)
                    .OfType<PlayerStatistic_CS2>()
                    .ToList();

                if (teamAStats.Any() && teamBStats.Any())
                {
                    // Use rounds played to determine winner as fallback
                    var teamARounds = teamAStats.FirstOrDefault()?.RoundsPlayed ?? 0;
                    var teamBRounds = teamBStats.FirstOrDefault()?.RoundsPlayed ?? 0;
                    
                    if (teamARounds > teamBRounds)
                        return match.TeamA;
                    else if (teamBRounds > teamARounds)
                        return match.TeamB;
                }
            }

            // Fallback: return null if we can't determine the winner
            return null;
        }

        private async Task AdvanceWinnerToNextSeries(Series currentSeries)
        {
            Console.WriteLine($"🚀 Attempting to advance winner from series {currentSeries.Id}");
            Console.WriteLine($"🚀 Series finished: {currentSeries.isFinished}, NextSeriesId: {currentSeries.NextSeriesId}");
            
            if (!currentSeries.isFinished || currentSeries.NextSeriesId == null)
            {
                Console.WriteLine($"❌ Cannot advance - series not finished or no next series");
                return;
            }

            var nextSeries = await _db.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .FirstOrDefaultAsync(s => s.Id == currentSeries.NextSeriesId);

            if (nextSeries == null)
            {
                Console.WriteLine($"❌ Next series not found with ID: {currentSeries.NextSeriesId}");
                return;
            }

            Console.WriteLine($"🎯 Next series found: {nextSeries.Id}");

            // Determine the winner of the current series
            var winner = DetermineSeriesWinner(currentSeries);
            if (winner == null)
            {
                Console.WriteLine($"❌ Could not determine series winner");
                return;
            }

            Console.WriteLine($"🏆 Series winner: {winner.FullName}");

            // Determine which team slot to fill in the next series
            var teamSlot = DetermineNextSeriesTeamSlot(currentSeries, nextSeries);
            Console.WriteLine($"🎯 Team slot to fill: {teamSlot}");

            //teamSlot == "TeamA" && 
            if (nextSeries.TeamAId == null)
            {
                nextSeries.TeamAId = winner.Id;
                nextSeries.TeamA = winner;
                Console.WriteLine($"✅ Advanced {winner.FullName} to TeamA of series {nextSeries.Id}");
            }
            //teamSlot == "TeamB" && 
            else if (nextSeries.TeamBId == null)
            {
                nextSeries.TeamBId = winner.Id;
                nextSeries.TeamB = winner;
                Console.WriteLine($"✅ Advanced {winner.FullName} to TeamB of series {nextSeries.Id}");
            }
            else if (teamSlot == "TeamA" && nextSeries.TeamAId != null)
            {
                Console.WriteLine($"❌ Cannot advance to TeamA - slot already filled by {nextSeries.TeamA?.FullName}");
            }
            else if (teamSlot == "TeamB" && nextSeries.TeamBId != null)
            {
                Console.WriteLine($"❌ Cannot advance to TeamB - slot already filled by {nextSeries.TeamB?.FullName}");
            }
            else
            {
                Console.WriteLine($"❌ Invalid team slot: {teamSlot}");
            }

            await _db.SaveChangesAsync();
        }

        private Team DetermineSeriesWinner(Series series)
        {
            Console.WriteLine($"🔍 Determining series winner for series {series.Id}");
            Console.WriteLine($"🔍 Series finished: {series.isFinished}, Matches count: {series.Matches.Count}");
            
            if (!series.isFinished || !series.Matches.Any())
            {
                Console.WriteLine($"❌ Series not finished or no matches");
                return null;
            }

            // Count wins for each team
            int teamAWins = 0;
            int teamBWins = 0;

            foreach (var match in series.Matches)
            {
                if (match.IsCompleted)
                {
                    var winner = DetermineMatchWinner(match);
                    Console.WriteLine($"🔍 Match {match.Id} winner: {winner?.FullName ?? "null"}");
                    
                    if (winner == series.TeamA)
                    {
                        teamAWins++;
                        Console.WriteLine($"✅ TeamA wins! Total: {teamAWins}");
                    }
                    else if (winner == series.TeamB)
                    {
                        teamBWins++;
                        Console.WriteLine($"✅ TeamB wins! Total: {teamBWins}");
                    }
                }
            }

            // Determine best-of format
            int totalMatches = series.Matches.Count;
            int winsNeeded = (totalMatches / 2) + 1;

            Console.WriteLine($"📊 Series {series.Id} - TeamA wins: {teamAWins}, TeamB wins: {teamBWins}, Wins needed: {winsNeeded}");

            if (teamAWins >= winsNeeded)
            {
                Console.WriteLine($"🏆 Series winner: {series.TeamA?.FullName}");
                return series.TeamA;
            }
            else if (teamBWins >= winsNeeded)
            {
                Console.WriteLine($"🏆 Series winner: {series.TeamB?.FullName}");
                return series.TeamB;
            }

            Console.WriteLine($"❌ No clear series winner yet");
            return null;
        }

        private string DetermineNextSeriesTeamSlot(Series currentSeries, Series nextSeries)
        {
            // For single elimination, it's straightforward
            if (currentSeries.Bracket == BracketType.Upper && nextSeries.Bracket == BracketType.Upper)
            {
                // Upper bracket progression - winner goes to TeamA of next series
                return "TeamA";
            }

            // For double elimination, we need to consider the bracket flow
            if (currentSeries.Bracket == BracketType.Upper && nextSeries.Bracket == BracketType.Upper)
            {
                // Upper bracket to upper bracket - winner goes to TeamA
                return "TeamA";
            }
            else if (currentSeries.Bracket == BracketType.Upper && nextSeries.Bracket == BracketType.Lower)
            {
                // Upper bracket loser drops to lower bracket
                // In double elimination, upper bracket losers drop to specific lower bracket positions
                return DetermineUpperBracketLoserDropSlot(currentSeries, nextSeries);
            }
            else if (currentSeries.Bracket == BracketType.Lower && nextSeries.Bracket == BracketType.Lower)
            {
                // Lower bracket progression
                return DetermineLowerBracketTeamSlot(currentSeries, nextSeries);
            }
            else if (currentSeries.Bracket == BracketType.Lower && nextSeries.Bracket == BracketType.GrandFinal)
            {
                // Lower bracket winner goes to Grand Final
                return "TeamB"; // Lower bracket winner is typically TeamB in Grand Final
            }
            else if (currentSeries.Bracket == BracketType.Upper && nextSeries.Bracket == BracketType.GrandFinal)
            {
                // Upper bracket winner goes to Grand Final
                return "TeamA"; // Upper bracket winner is typically TeamA in Grand Final
            }

            // Default fallback
            return "TeamA";
        }

        private string DetermineUpperBracketLoserDropSlot(Series currentSeries, Series nextSeries)
        {
            // In double elimination, upper bracket losers drop to specific lower bracket positions
            // The position depends on the round and position in the upper bracket
            
            // For now, use a simple approach based on position
            // In a real implementation, you'd need to map this based on the bracket structure
            if (currentSeries.Position % 2 == 1)
                return "TeamA";
            else
                return "TeamB";
        }

        private string DetermineLowerBracketTeamSlot(Series currentSeries, Series nextSeries)
        {
            // Lower bracket progression follows a specific pattern
            // Winners advance to the next round in the lower bracket
            
            // For now, use position-based logic
            // In a real implementation, you'd need to consider the bracket structure
            if (currentSeries.Position % 2 == 1)
                return "TeamA";
            else
                return "TeamB";
        }
    }
}
