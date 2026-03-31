namespace Balkana.Data.Models
{
    public class UserTrophy
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int TrophyId { get; set; }
        public Trophy Trophy { get; set; }

        public DateTime DateAwarded { get; set; } = DateTime.UtcNow;
    }
}

