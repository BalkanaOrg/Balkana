namespace Balkana.Data.Models
{
    public class GameProfile
    {
        public int Id { get; set; }
        public string UUID { get; set; }
        public string Provider {  get; set; } //Riot, Faceit, Steam

        public int PlayerId { get; set; }
        public Player Player { get; set; }
    }
}
