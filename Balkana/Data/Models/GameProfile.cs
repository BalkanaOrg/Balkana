namespace Balkana.Data.Models
{
    public class GameProfile
    {
        public int Id { get; set; }
        public string UUID { get; set; }
        public string Provider {  get; set; } //Riot, Faceit, Steam

        /// <summary>Optional label when a player has multiple accounts for the same provider (e.g. two FACEIT profiles).</summary>
        public string? DisplayName { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }
    }
}
