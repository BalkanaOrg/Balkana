using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Match;
using Balkana.Services.Matches;
using Balkana.Services.Matches.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Globalization;

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
        public async Task<IActionResult> ForfeitSeries(int seriesId, int winningTeamId)
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

            // Determine the winning team (the one selected to progress)
            Team winningTeam = null;
            Team losingTeam = null;
            if (winningTeamId == series.TeamAId)
            {
                winningTeam = series.TeamA;
                losingTeam = series.TeamB;
            }
            else if (winningTeamId == series.TeamBId)
            {
                winningTeam = series.TeamB;
                losingTeam = series.TeamA;
            }
            else
            {
                TempData["Error"] = "Invalid team specified for progression.";
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

            TempData["Success"] = $"{series.TeamA?.FullName} vs {series.TeamB?.FullName} - {winningTeam.FullName} advances (opponent forfeited).";
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

            // 🆕 NEW: Handle loser advancement for double elimination
            if (currentSeries.Bracket == BracketType.Upper && (nextSeries.Bracket == BracketType.Upper || nextSeries.Bracket == BracketType.GrandFinal))
            {
                // Upper bracket to upper bracket OR upper bracket to grand final - also need to handle the loser dropping to lower bracket
                await AdvanceLoserToLowerBracket(currentSeries);
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

        private async Task AdvanceLoserToLowerBracket(Series upperBracketSeries)
        {
            Console.WriteLine($"🔻 Attempting to advance loser from upper bracket series {upperBracketSeries.Id}");
            
            // Determine the loser of the upper bracket series
            var winner = DetermineSeriesWinner(upperBracketSeries);
            if (winner == null)
            {
                Console.WriteLine($"❌ Could not determine series winner, cannot determine loser");
                return;
            }

            var loser = upperBracketSeries.TeamA == winner ? upperBracketSeries.TeamB : upperBracketSeries.TeamA;
            if (loser == null)
            {
                Console.WriteLine($"❌ Could not determine series loser");
                return;
            }

            Console.WriteLine($"🔻 Series loser: {loser.FullName}");

            // Find the corresponding lower bracket series for this upper bracket series
            var lowerBracketSeries = await FindCorrespondingLowerBracketSeries(upperBracketSeries);
            if (lowerBracketSeries == null)
            {
                Console.WriteLine($"❌ Could not find corresponding lower bracket series for upper bracket series {upperBracketSeries.Id}");
                
                // Check if this is a bye match (no loser to seed)
                if (upperBracketSeries.TeamAId != null && upperBracketSeries.TeamBId == null)
                {
                    Console.WriteLine($"✅ This is a bye match - no loser to seed into lower bracket");
                    return;
                }
                
                Console.WriteLine($"❌ No lower bracket series found and this is not a bye match");
                return;
            }

            Console.WriteLine($"🎯 Found corresponding lower bracket series: {lowerBracketSeries.Id}");

            // Seed the loser into the lower bracket series
            if (lowerBracketSeries.TeamAId == null)
            {
                lowerBracketSeries.TeamAId = loser.Id;
                lowerBracketSeries.TeamA = loser;
                Console.WriteLine($"✅ Seeded loser {loser.FullName} to TeamA of lower bracket series {lowerBracketSeries.Id}");
            }
            else if (lowerBracketSeries.TeamBId == null)
            {
                lowerBracketSeries.TeamBId = loser.Id;
                lowerBracketSeries.TeamB = loser;
                Console.WriteLine($"✅ Seeded loser {loser.FullName} to TeamB of lower bracket series {lowerBracketSeries.Id}");
            }
            else
            {
                Console.WriteLine($"❌ Lower bracket series {lowerBracketSeries.Id} already has both teams filled");
            }

            await _db.SaveChangesAsync();
        }

        private async Task<Series> FindCorrespondingLowerBracketSeries(Series upperBracketSeries)
        {
            // Special case: WB Final loser goes to LB Final
            if (upperBracketSeries.Round == 3) // WB Final is typically Round 3
            {
                var lowerBracketFinal = await _db.Series
                    .Include(s => s.TeamA)
                    .Include(s => s.TeamB)
                    .Where(s => s.TournamentId == upperBracketSeries.TournamentId &&
                               s.Bracket == BracketType.Lower &&
                               s.Round == 4) // LB Final is typically Round 4
                    .FirstOrDefaultAsync();

                if (lowerBracketFinal != null)
                {
                    Console.WriteLine($"🎯 Found LB Final for WB Final loser: LB Round {lowerBracketFinal.Round}");
                    return lowerBracketFinal;
                }
            }

            // In double elimination, upper bracket round N losers drop to lower bracket round N
            // We need to find the lower bracket series that corresponds to this upper bracket series
            
            var lowerBracketSeries = await _db.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Where(s => s.TournamentId == upperBracketSeries.TournamentId &&
                           s.Bracket == BracketType.Lower &&
                           s.Round == upperBracketSeries.Round &&
                           s.Position == upperBracketSeries.Position)
                .FirstOrDefaultAsync();

            if (lowerBracketSeries != null)
            {
                Console.WriteLine($"🎯 Found exact match: LB Round {lowerBracketSeries.Round} Match {lowerBracketSeries.Position}");
                return lowerBracketSeries;
            }

            // If no exact match, try to find the appropriate lower bracket series based on position
            // Need to handle different bracket sizes and bye scenarios
            
            int lowerBracketPosition;
            if (upperBracketSeries.Round == 1)
            {
                // For first round, we need to handle different bracket sizes
                lowerBracketPosition = CalculateLowerBracketPositionForFirstRound(upperBracketSeries);
            }
            else
            {
                // Other rounds: 1 upper bracket match feeds into 1 lower bracket match
                lowerBracketPosition = upperBracketSeries.Position;
            }

            lowerBracketSeries = await _db.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Where(s => s.TournamentId == upperBracketSeries.TournamentId &&
                           s.Bracket == BracketType.Lower &&
                           s.Round == upperBracketSeries.Round &&
                           s.Position == lowerBracketPosition)
                .FirstOrDefaultAsync();

            if (lowerBracketSeries != null)
            {
                Console.WriteLine($"🎯 Found position-based match: LB Round {lowerBracketSeries.Round} Match {lowerBracketSeries.Position}");
            }

            return lowerBracketSeries;
        }


        private int CalculateLowerBracketPositionForFirstRound(Series upperBracketSeries)
        {
            // Get all upper bracket round 1 series to understand the bracket structure
            var allUpperRound1Series = _db.Series
                .Where(s => s.TournamentId == upperBracketSeries.TournamentId &&
                           s.Bracket == BracketType.Upper &&
                           s.Round == 1)
                .OrderBy(s => s.Position)
                .ToList();

            // Check if this upper bracket series has a bye (only one team)
            bool hasBye = upperBracketSeries.TeamAId != null && upperBracketSeries.TeamBId == null;
            
            Console.WriteLine($"🔍 UB Match {upperBracketSeries.Position}: Has bye = {hasBye}");

            if (hasBye)
            {
                Console.WriteLine($"❌ UB Match {upperBracketSeries.Position} has bye - no loser to seed");
                return -1; // No corresponding lower bracket series
            }

            // Count how many upper bracket matches actually have losers (no byes)
            var matchesWithLosers = allUpperRound1Series.Where(s => s.TeamAId != null && s.TeamBId != null).ToList();
            int loserMatchIndex = matchesWithLosers.FindIndex(s => s.Id == upperBracketSeries.Id);
            
            Console.WriteLine($"🔍 UB Match {upperBracketSeries.Position} is loser match #{loserMatchIndex + 1} of {matchesWithLosers.Count}");

            // For 7-team bracket: 3 matches with losers, all should play in LB Round 1
            // The correct mapping should be:
            // UB Match 1 loser (TDI bye - no loser) → No LB match
            // UB Match 2 loser (Banana B) → LB Match 2 (LB 1.2)
            // UB Match 3 loser (VAGMASTERS) → LB Match 1 (LB 1.1)
            // UB Match 4 loser (Bulletproof) → LB Match 1 (LB 1.1)
            if (matchesWithLosers.Count == 3)
            {
                // 7-team bracket: Correct lower bracket seeding
                if (upperBracketSeries.Position == 2) // UB Match 2 - Banana B vs Divi Qzovci
                    return 2; // LB Match 2 (LB 1.2) - Banana B goes here
                else if (upperBracketSeries.Position == 3) // UB Match 3 - Ribarite vs VAGMASTERS
                    return 1; // LB Match 1 (LB 1.1) - VAGMASTERS goes here
                else if (upperBracketSeries.Position == 4) // UB Match 4 - SOP Clan vs Bulletproof
                    return 1; // LB Match 1 (LB 1.1) - Bulletproof goes here (same match as VAGMASTERS)
                else
                    return 1; // Default
            }
            else if (matchesWithLosers.Count == 2)
            {
                // 6-team bracket: 2 losers, 1 LB match
                return 1; // All losers go to LB Match 1
            }
            else
            {
                // Standard mapping: 2 upper bracket matches feed into 1 lower bracket match
                return ((loserMatchIndex) / 2) + 1;
            }
        }

        // Manual Statistics Upload
        [HttpGet]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> ManualStatsUpload()
        {
            var vm = new ManualStatsUploadViewModel
            {
                Tournaments = await _db.Tournaments
                    .Include(t => t.Game)
                    .Where(t => t.Game.ShortName == "CS2")
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.FullName
                    })
                    .ToListAsync(),
                Maps = await _db.GameMaps
                    .Where(m => m.Game.ShortName == "CS2")
                    .Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = m.Name
                    })
                    .ToListAsync(),
                Players = await _db.Players
                    .Include(p => p.GameProfiles)
                    .Where(p => p.GameProfiles.Any(gp => gp.Provider == "Faceit"))
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"{p.Nickname} ({p.FirstName} {p.LastName})"
                    })
                    .ToListAsync(),
                Teams = await _db.Teams
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.FullName
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> ManualStatsUpload(ManualStatsUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdowns
                model.Tournaments = await _db.Tournaments
                    .Include(t => t.Game)
                    .Where(t => t.Game.ShortName == "CS2")
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.FullName
                    })
                    .ToListAsync();
                model.Maps = await _db.GameMaps
                    .Where(m => m.Game.ShortName == "CS2")
                    .Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = m.Name
                    })
                    .ToListAsync();
                model.Players = await _db.Players
                    .Include(p => p.GameProfiles)
                    .Where(p => p.GameProfiles.Any(gp => gp.Provider == "Faceit"))
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"{p.Nickname} ({p.FirstName} {p.LastName})"
                    })
                    .ToListAsync();
                model.Teams = await _db.Teams
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.FullName
                    })
                    .ToListAsync();
                return View(model);
            }

            // Get the teams (initial selection)
            var teamA = await _db.Teams.FirstOrDefaultAsync(t => t.Id == model.TeamAId);
            var teamB = await _db.Teams.FirstOrDefaultAsync(t => t.Id == model.TeamBId);

            if (teamA == null || teamB == null)
            {
                ModelState.AddModelError("", "Invalid teams selected.");
                return View(model);
            }

            // Resolve teams based on selected players and transfers at match date (fallback to selected team)
            var resolvedTeamA = await ResolveTeamFromPlayersAsync(
                new[] { model.TeamAPlayer1Id, model.TeamAPlayer2Id, model.TeamAPlayer3Id, model.TeamAPlayer4Id, model.TeamAPlayer5Id },
                model.PlayedAt);

            var resolvedTeamB = await ResolveTeamFromPlayersAsync(
                new[] { model.TeamBPlayer1Id, model.TeamBPlayer2Id, model.TeamBPlayer3Id, model.TeamBPlayer4Id, model.TeamBPlayer5Id },
                model.PlayedAt);

            if (resolvedTeamA != null) teamA = resolvedTeamA;
            if (resolvedTeamB != null) teamB = resolvedTeamB;

            // Create the match
            var match = new MatchCS
            {
                ExternalMatchId = $"MANUAL-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Source = "MANUAL",
                PlayedAt = model.PlayedAt,
                IsCompleted = true,
                CompetitionType = model.CompetitionType,
                MapId = model.MapId,
                TeamA = teamA,
                TeamAId = teamA.Id,
                TeamASourceSlot = "TeamA",
                TeamB = teamB,
                TeamBId = teamB.Id,
                TeamBSourceSlot = "TeamB",
                SeriesId = model.SeriesId,
                
                // Round information
                TeamARounds = model.TeamARounds,
                TeamBRounds = model.TeamBRounds,
                TotalRounds = model.TotalRounds,
                
                PlayerStats = new List<PlayerStatistic>()
            };

            // Set winner
            if (model.WinningTeam == "TeamA")
            {
                match.WinnerTeam = teamA;
                match.WinnerTeamId = teamA.Id;
            }
            else
            {
                match.WinnerTeam = teamB;
                match.WinnerTeamId = teamB.Id;
            }

            // Get all player IDs and their Faceit UUIDs
            var playerIds = new List<int>
            {
                model.TeamAPlayer1Id, model.TeamAPlayer2Id, model.TeamAPlayer3Id, model.TeamAPlayer4Id, model.TeamAPlayer5Id,
                model.TeamBPlayer1Id, model.TeamBPlayer2Id, model.TeamBPlayer3Id, model.TeamBPlayer4Id, model.TeamBPlayer5Id
            };

            var players = await _db.Players
                .Include(p => p.GameProfiles)
                .Where(p => playerIds.Contains(p.Id))
                .ToListAsync();

            var playerUuidMap = players.ToDictionary(
                p => p.Id,
                p => p.GameProfiles.FirstOrDefault(gp => gp.Provider.Equals("FACEIT", StringComparison.OrdinalIgnoreCase))?.UUID ?? ""
            );

            // Create player statistics for Team A
            var teamAPlayerStats = new[]
            {
                model.TeamAPlayer1Stats, model.TeamAPlayer2Stats, model.TeamAPlayer3Stats, model.TeamAPlayer4Stats, model.TeamAPlayer5Stats
            };
            var teamAPlayerIds = new[]
            {
                model.TeamAPlayer1Id, model.TeamAPlayer2Id, model.TeamAPlayer3Id, model.TeamAPlayer4Id, model.TeamAPlayer5Id
            };

            for (int i = 0; i < teamAPlayerStats.Length; i++)
            {
                var stats = teamAPlayerStats[i];
                var playerId = teamAPlayerIds[i];
                var uuid = playerUuidMap.GetValueOrDefault(playerId, "");

                var playerStat = new PlayerStatistic_CS2
                {
                    PlayerUUID = uuid,
                    Source = "MANUAL",
                    Team = "TeamA",
                    IsWinner = model.WinningTeam == "TeamA",
                    Kills = stats.Kills,
                    Assists = stats.Assists,
                    Deaths = stats.Deaths,
                    Damage = stats.Damage,
                    TsideRoundsWon = stats.TsideRoundsWon,
                    CTsideRoundsWon = stats.CTsideRoundsWon,
                    RoundsPlayed = stats.RoundsPlayed,
                    KAST = stats.KAST,
                    HSkills = stats.HSkills,
                    HLTV1 = stats.HLTV1,
                    UD = stats.UD,
                    FK = stats.FK,
                    FD = stats.FD,
                    _1k = stats._1k,
                    _2k = stats._2k,
                    _3k = stats._3k,
                    _4k = stats._4k,
                    _5k = stats._5k,
                    _1v1 = stats._1v1,
                    _1v2 = stats._1v2,
                    _1v3 = stats._1v3,
                    _1v4 = stats._1v4,
                    _1v5 = stats._1v5,
                    SniperKills = stats.SniperKills,
                    PistolKills = stats.PistolKills,
                    KnifeKills = stats.KnifeKills,
                    Flashes = stats.Flashes
                };

                match.PlayerStats.Add(playerStat);
            }

            // Create player statistics for Team B
            var teamBPlayerStats = new[]
            {
                model.TeamBPlayer1Stats, model.TeamBPlayer2Stats, model.TeamBPlayer3Stats, model.TeamBPlayer4Stats, model.TeamBPlayer5Stats
            };
            var teamBPlayerIds = new[]
            {
                model.TeamBPlayer1Id, model.TeamBPlayer2Id, model.TeamBPlayer3Id, model.TeamBPlayer4Id, model.TeamBPlayer5Id
            };

            for (int i = 0; i < teamBPlayerStats.Length; i++)
            {
                var stats = teamBPlayerStats[i];
                var playerId = teamBPlayerIds[i];
                var uuid = playerUuidMap.GetValueOrDefault(playerId, "");

                var playerStat = new PlayerStatistic_CS2
                {
                    PlayerUUID = uuid,
                    Source = "MANUAL",
                    Team = "TeamB",
                    IsWinner = model.WinningTeam == "TeamB",
                    Kills = stats.Kills,
                    Assists = stats.Assists,
                    Deaths = stats.Deaths,
                    Damage = stats.Damage,
                    TsideRoundsWon = stats.TsideRoundsWon,
                    CTsideRoundsWon = stats.CTsideRoundsWon,
                    RoundsPlayed = stats.RoundsPlayed,
                    KAST = stats.KAST,
                    HSkills = stats.HSkills,
                    HLTV1 = stats.HLTV1,
                    UD = stats.UD,
                    FK = stats.FK,
                    FD = stats.FD,
                    _1k = stats._1k,
                    _2k = stats._2k,
                    _3k = stats._3k,
                    _4k = stats._4k,
                    _5k = stats._5k,
                    _1v1 = stats._1v1,
                    _1v2 = stats._1v2,
                    _1v3 = stats._1v3,
                    _1v4 = stats._1v4,
                    _1v5 = stats._1v5,
                    SniperKills = stats.SniperKills,
                    PistolKills = stats.PistolKills,
                    KnifeKills = stats.KnifeKills,
                    Flashes = stats.Flashes
                };

                match.PlayerStats.Add(playerStat);
            }

            // Save the match
            _db.Matches.Add(match);
            await _db.SaveChangesAsync();

            // If series is provided, update series winner and advance to next series
            if (model.SeriesId > 0)
            {
                var series = await _db.Series
                    .Include(s => s.TeamA)
                    .Include(s => s.TeamB)
                    .FirstOrDefaultAsync(s => s.Id == model.SeriesId);

                if (series != null)
                {
                    await UpdateSeriesWinner(series, new List<Match> { match });
                    await AdvanceWinnerToNextSeries(series);
                }
            }

            TempData["Success"] = "Match statistics uploaded successfully!";
            return RedirectToAction("Index", "MatchHistory");
        }

        [HttpGet]
        public async Task<IActionResult> GetSeriesForTournament(int tournamentId)
        {
            var series = await _db.Series
                .Include(s => s.TeamA)
                .Include(s => s.TeamB)
                .Where(s => s.TournamentId == tournamentId)
                .Select(s => new
                {
                    id = s.Id,
                    text = $"{s.TeamA.FullName} vs {s.TeamB.FullName} - {s.Name}"
                })
                .ToListAsync();

            return Json(series);
        }

        [HttpGet]
        public async Task<IActionResult> SearchPlayers(string term)
        {
            var query = _db.Players
                .Include(p => p.GameProfiles)
                .Where(p => p.GameProfiles.Any(gp => gp.Provider == "Faceit"));

            // If term is provided and has at least 2 characters, filter by it
            if (!string.IsNullOrEmpty(term) && term.Length >= 2)
            {
                query = query.Where(p => p.Nickname.Contains(term) || p.FirstName.Contains(term) || p.LastName.Contains(term));
            }

            var players = await query
                .Take(50) // Increased limit for initial load
                .Select(p => new
                {
                    id = p.Id,
                    text = $"{p.Nickname} ({p.FirstName} {p.LastName})",
                    nickname = p.Nickname,
                    fullName = $"{p.FirstName} {p.LastName}"
                })
                .ToListAsync();

            return Json(players);
        }

        [HttpGet]
        public async Task<IActionResult> SearchTeams(string term)
        {
            var query = _db.Teams.AsQueryable();

            // If term is provided and has at least 2 characters, filter by it
            if (!string.IsNullOrEmpty(term) && term.Length >= 2)
            {
                query = query.Where(t => t.FullName.Contains(term) || t.Tag.Contains(term));
            }

            var teams = await query
                .Take(20)
                .Select(t => new
                {
                    id = t.Id,
                    text = t.FullName,
                    tag = t.Tag
                })
                .ToListAsync();

            return Json(teams);
        }

        [HttpGet]
        public async Task<IActionResult> GetTournamentDetails(int tournamentId)
        {
            var tournament = await _db.Tournaments
                .Where(t => t.Id == tournamentId)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.FullName,
                    startDate = t.StartDate,
                    endDate = t.EndDate
                })
                .FirstOrDefaultAsync();

            if (tournament == null)
            {
                return Json(new { error = "Tournament not found" });
            }

            return Json(tournament);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Authorize(Roles = "Administrator,Moderator")]
        public IActionResult ParsePopflashStats([FromBody] ParsePopflashRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PastedText))
            {
                return BadRequest(new { error = "Pasted text is required" });
            }

            try
            {
                var parser = new PopflashStatsParser();
                var parsedStats = parser.Parse(request.PastedText);

                return Json(new { success = true, data = parsedStats });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Failed to parse stats: {ex.Message}" });
            }
        }

        public class ParsePopflashRequest
        {
            public string PastedText { get; set; } = string.Empty;
        }

        /// <summary>
        /// Resolve a team by checking transfers for the provided players that were active at the match date.
        /// Mirrors the Faceit importer logic but uses player IDs from the manual form.
        /// </summary>
        private async Task<Team?> ResolveTeamFromPlayersAsync(IEnumerable<int> playerIds, DateTime matchDate)
        {
            var ids = playerIds.Where(id => id > 0).ToList();
            if (!ids.Any()) return null;

            var teams = await _db.Teams
                .Include(t => t.Transfers)
                    .ThenInclude(tr => tr.Player)
                        .ThenInclude(p => p.GameProfiles)
                .ToListAsync();

            Team? bestMatch = null;
            int maxMatches = 0;

            foreach (var team in teams)
            {
                var matchingPlayers = team.Transfers
                    .Where(tr =>
                        ids.Contains(tr.PlayerId) &&
                        tr.Status == PlayerTeamStatus.Active &&
                        tr.StartDate <= matchDate &&
                        (tr.EndDate == null || tr.EndDate >= matchDate))
                    .Count();

                if (matchingPlayers > maxMatches)
                {
                    maxMatches = matchingPlayers;
                    bestMatch = team;
                }
            }

            return bestMatch;
        }
    }
}
