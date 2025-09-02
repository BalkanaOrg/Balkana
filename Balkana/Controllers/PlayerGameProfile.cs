namespace Balkana.Controllers
{
    public class PlayerGameProfile
    {
        public int Id { get; set; }
        public string Identifier { get; set; } // puuid or player_id
        public string AccountType { get; set; } // Riot or FaceIt
    }
}
