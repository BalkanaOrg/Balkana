namespace Balkana.Data.Models
{
    public class Trophy
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconURL { get; set; }
        public string AwardType { get; set; } // "MVP", "EVP", "Balkana Awards", etc.

        public DateTime AwardDate { get; set; }

        public ICollection<PlayerTrophy> PlayerTrophies { get; set; } = new List<PlayerTrophy>();
        public ICollection<TeamTrophy> TeamTrophies { get; set; } = new List<TeamTrophy>();
    }
}
