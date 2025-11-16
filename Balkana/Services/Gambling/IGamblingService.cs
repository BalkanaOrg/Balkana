using Balkana.Models.Gambling;

namespace Balkana.Services.Gambling
{
    public interface IGamblingService
    {
        Task SaveSlotMachineSessionAsync(string userId, SlotMachineViewModel model);
        Task<List<GamblingSessionViewModel>> GetUserGamblingHistoryAsync(string userId);
        Task<List<GamblingLeaderboardEntryViewModel>> GetGamblingLeaderboardAsync();
        Task<decimal> GetUserTotalWinningsAsync(string userId);
        Task<int> GetUserTotalSessionsAsync(string userId);
    }
}
