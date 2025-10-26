using Balkana.Data;
using Balkana.Models.Gambling;
using Balkana.Services.Gambling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Balkana.Controllers
{
    public class GamblingController : Controller
    {
        private readonly IGamblingService _gamblingService;
        private readonly ApplicationDbContext _context;

        public GamblingController(IGamblingService gamblingService, ApplicationDbContext context)
        {
            _gamblingService = gamblingService;
            _context = context;
        }

        // GET: /Gambling
        public IActionResult Index()
        {
            var model = new GamblingIndexViewModel
            {
                AvailableGames = new List<GamblingGameViewModel>
                {
                    new GamblingGameViewModel
                    {
                        Id = 1,
                        Name = "Slot Machine",
                        Description = "Classic 3-reel slot machine with various symbols",
                        ImageUrl = "/images/slot-machine.png",
                        IsAvailable = true,
                        MinBet = 1,
                        MaxBet = 100
                    }
                }
            };

            return View(model);
        }

        // GET: /Gambling/SlotMachine
        public IActionResult SlotMachine()
        {
            var model = new SlotMachineViewModel
            {
                Reels = new List<string> { "🍒", "🍋", "🍊", "🍇", "🔔", "⭐", "💎", "7️⃣" },
                CurrentSymbols = new List<string> { "🍒", "🍒", "🍒" },
                IsSpinning = false,
                Credits = 100,
                BetAmount = 1,
                LastWin = 0,
                TotalWins = 0,
                TotalSpins = 0
            };

            return View(model);
        }

        // POST: /Gambling/SpinSlotMachine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SpinSlotMachine(SlotMachineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("SlotMachine", model);
            }

            // Check if user has enough credits
            if (model.Credits < model.BetAmount)
            {
                TempData["ErrorMessage"] = "Insufficient credits!";
                return View("SlotMachine", model);
            }

            // Deduct bet amount
            model.Credits -= model.BetAmount;
            model.TotalSpins++;

            // Generate random symbols
            var random = new Random();
            model.CurrentSymbols = new List<string>
            {
                model.Reels[random.Next(model.Reels.Count)],
                model.Reels[random.Next(model.Reels.Count)],
                model.Reels[random.Next(model.Reels.Count)]
            };

            // Calculate win
            model.LastWin = CalculateSlotWin(model.CurrentSymbols, model.BetAmount);
            model.Credits += model.LastWin;
            model.TotalWins += model.LastWin;

            // Set spinning state
            model.IsSpinning = true;

            // Save gambling session if user is authenticated
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _gamblingService.SaveSlotMachineSessionAsync(userId, model);
            }

            TempData["SuccessMessage"] = model.LastWin > 0 
                ? $"Congratulations! You won {model.LastWin} credits!" 
                : "Better luck next time!";

            return View("SlotMachine", model);
        }

        // GET: /Gambling/History
        [Authorize]
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessions = await _gamblingService.GetUserGamblingHistoryAsync(userId);

            var model = new GamblingHistoryViewModel
            {
                Sessions = sessions,
                TotalWagered = sessions.Sum(s => s.BetAmount),
                TotalWon = sessions.Sum(s => s.WinAmount),
                NetResult = sessions.Sum(s => s.WinAmount) - sessions.Sum(s => s.BetAmount)
            };

            return View(model);
        }

        // GET: /Gambling/Leaderboard
        public async Task<IActionResult> Leaderboard()
        {
            var leaderboard = await _gamblingService.GetGamblingLeaderboardAsync();
            
            var model = new GamblingLeaderboardViewModel
            {
                TopPlayers = leaderboard,
                LastUpdated = DateTime.UtcNow
            };

            return View(model);
        }

        private int CalculateSlotWin(List<string> symbols, int betAmount)
        {
            // All three symbols match
            if (symbols[0] == symbols[1] && symbols[1] == symbols[2])
            {
                return symbols[0] switch
                {
                    "💎" => betAmount * 100, // Diamond - highest payout
                    "7️⃣" => betAmount * 50,  // Seven
                    "⭐" => betAmount * 25,   // Star
                    "🔔" => betAmount * 15,  // Bell
                    "🍇" => betAmount * 10,  // Grapes
                    "🍊" => betAmount * 8,   // Orange
                    "🍋" => betAmount * 6,   // Lemon
                    "🍒" => betAmount * 5,   // Cherry
                    _ => betAmount * 3
                };
            }

            // Two symbols match
            if (symbols[0] == symbols[1] || symbols[1] == symbols[2] || symbols[0] == symbols[2])
            {
                return betAmount * 2;
            }

            // No win
            return 0;
        }
    }
}
