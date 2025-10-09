using Balkana.Services.Transfers.Models;

namespace Balkana.Services.Players.Models
{
    public class PlayerDetailsServiceModel : PlayerServiceModel
    {
        // ✅ Player has many pictures, but we'll mainly show the latest
        public IEnumerable<PlayerPictureServiceModel> PlayerPictures { get; set; }
            = new List<PlayerPictureServiceModel>();

        // ✅ Nationality info
        public string NationalityName { get; set; }
        public string FlagUrl { get; set; }

        // ✅ Game profiles for filtering
        public List<PlayerGameProfileServiceModel> GameProfiles { get; set; } = new();

        // ✅ Transfers grouped by game
        public Dictionary<string, List<TransferDetailsServiceModel>> TransfersByGame { get; set; }
        = new Dictionary<string, List<TransferDetailsServiceModel>>();

        // ✅ Average stats by game
        public Dictionary<string, PlayerAverageStatsServiceModel> AverageStatsByGame { get; set; }
        = new Dictionary<string, PlayerAverageStatsServiceModel>();

        // ✅ Tournament participation by game
        public Dictionary<string, List<PlayerTournamentParticipationServiceModel>> TournamentParticipationByGame { get; set; }
        = new Dictionary<string, List<PlayerTournamentParticipationServiceModel>>();

        // ✅ Match history by game
        public Dictionary<string, PlayerMatchHistoryServiceModel> MatchHistoryByGame { get; set; }
        = new Dictionary<string, PlayerMatchHistoryServiceModel>();
    }
    
    public class PlayerGameProfileServiceModel
    {
        public string Provider { get; set; } // "FACEIT", "RIOT", etc.
        public string GameName { get; set; } // "Counter-Strike", "League of Legends", etc.
        public string UUID { get; set; }
    }
}
