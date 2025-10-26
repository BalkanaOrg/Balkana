namespace Balkana.Models.Gambling
{
    public class GamblingIndexViewModel
    {
        public List<GamblingGameViewModel> AvailableGames { get; set; } = new List<GamblingGameViewModel>();
        public int UserCredits { get; set; } = 100;
        public bool IsAuthenticated { get; set; }
    }

    public class GamblingGameViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public decimal MinBet { get; set; }
        public decimal MaxBet { get; set; }
        public string GameType { get; set; }
    }

    public class SlotMachineViewModel
    {
        public List<string> Reels { get; set; } = new List<string>();
        public List<string> CurrentSymbols { get; set; } = new List<string>();
        public bool IsSpinning { get; set; }
        public int Credits { get; set; }
        public int BetAmount { get; set; }
        public int LastWin { get; set; }
        public int TotalWins { get; set; }
        public int TotalSpins { get; set; }
        public string Message { get; set; }
        public bool IsWin { get; set; }
    }

    public class GamblingHistoryViewModel
    {
        public List<GamblingSessionViewModel> Sessions { get; set; } = new List<GamblingSessionViewModel>();
        public decimal TotalWagered { get; set; }
        public decimal TotalWon { get; set; }
        public decimal NetResult { get; set; }
        public int TotalSessions { get; set; }
    }

    public class GamblingSessionViewModel
    {
        public int Id { get; set; }
        public string GameType { get; set; }
        public decimal BetAmount { get; set; }
        public decimal WinAmount { get; set; }
        public DateTime PlayedAt { get; set; }
        public string Result { get; set; }
        public bool IsWin { get; set; }
    }

    public class GamblingLeaderboardViewModel
    {
        public List<GamblingLeaderboardEntryViewModel> TopPlayers { get; set; } = new List<GamblingLeaderboardEntryViewModel>();
        public DateTime LastUpdated { get; set; }
        public string Timeframe { get; set; } = "All Time";
    }

    public class GamblingLeaderboardEntryViewModel
    {
        public string PlayerName { get; set; }
        public decimal TotalWinnings { get; set; }
        public int TotalSessions { get; set; }
        public decimal WinRate { get; set; }
        public string BiggestWin { get; set; }
        public int Rank { get; set; }
    }
}
