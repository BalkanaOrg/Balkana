namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceItPlayerDTO
    {
        public string player_id { get; set; } // internal FACEIT player ID
        public string nickname { get; set; }
        public string avatar { get; set; }
        public string game_player_id { get; set; } // SteamID64 for CS2
        public string game_player_name { get; set; }
        public string game_player_platform { get; set; }
        public string country { get; set; }

        public Dictionary<string, string> stats { get; set; }
    }
}
