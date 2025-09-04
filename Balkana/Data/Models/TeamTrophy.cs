namespace Balkana.Data.Models
{
    public class TeamTrophy
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public Team Team { get; set; }
        public int TrophyId { get; set; }
        public Trophy Trophy { get; set; }
    }
}
