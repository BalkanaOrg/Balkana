namespace Balkana.Data.Models
{
    public class PlayerTrophy
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public Player Player { get; set; }
        public int TrophyId { get; set; }
        public Trophy Trophy { get; set; }
    }
}
