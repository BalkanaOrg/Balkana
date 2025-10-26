using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Gambling;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Gambling
{
    public class GamblingService : IGamblingService
    {
        private readonly ApplicationDbContext _context;

        public GamblingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveSlotMachineSessionAsync(string userId, SlotMachineViewModel model)
        {
            var session = new GamblingSession
            {
                UserId = userId,
                GameType = "SlotMachine",
                BetAmount = model.BetAmount,
                WinAmount = model.LastWin,
                PlayedAt = DateTime.UtcNow,
                Result = string.Join(",", model.CurrentSymbols),
                IsWin = model.LastWin > 0
            };

            _context.GamblingSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task<List<GamblingSessionViewModel>> GetUserGamblingHistoryAsync(string userId)
        {
            var sessions = await _context.GamblingSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.PlayedAt)
                .Take(50)
                .Select(s => new GamblingSessionViewModel
                {
                    Id = s.Id,
                    GameType = s.GameType,
                    BetAmount = s.BetAmount,
                    WinAmount = s.WinAmount,
                    PlayedAt = s.PlayedAt,
                    Result = s.Result,
                    IsWin = s.IsWin
                })
                .ToListAsync();

            return sessions;
        }

        public async Task<List<GamblingLeaderboardEntryViewModel>> GetGamblingLeaderboardAsync()
        {
            var leaderboard = await _context.GamblingSessions
                .GroupBy(s => s.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalWinnings = g.Sum(s => s.WinAmount),
                    TotalSessions = g.Count(),
                    BiggestWin = g.Max(s => s.WinAmount),
                    WinRate = g.Count(s => s.IsWin) * 100.0 / g.Count()
                })
                .OrderByDescending(x => x.TotalWinnings)
                .Take(10)
                .ToListAsync();

            var result = new List<GamblingLeaderboardEntryViewModel>();
            int rank = 1;

            foreach (var entry in leaderboard)
            {
                var user = await _context.Users.FindAsync(entry.UserId);
                result.Add(new GamblingLeaderboardEntryViewModel
                {
                    PlayerName = user?.UserName ?? "Anonymous",
                    TotalWinnings = entry.TotalWinnings,
                    TotalSessions = entry.TotalSessions,
                    WinRate = (decimal)entry.WinRate,
                    BiggestWin = entry.BiggestWin.ToString("F2"),
                    Rank = rank++
                });
            }

            return result;
        }

        public async Task<decimal> GetUserTotalWinningsAsync(string userId)
        {
            return await _context.GamblingSessions
                .Where(s => s.UserId == userId)
                .SumAsync(s => s.WinAmount);
        }

        public async Task<int> GetUserTotalSessionsAsync(string userId)
        {
            return await _context.GamblingSessions
                .Where(s => s.UserId == userId)
                .CountAsync();
        }
    }
}
