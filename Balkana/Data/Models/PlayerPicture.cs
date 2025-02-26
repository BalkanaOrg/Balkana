namespace Balkana.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using static DataConstants;

    public class PlayerPicture
    {
        public int Id { get; set; }

        [Required]
        public string PictureURL { get; set; } //or emoji, idk

        public int PlayerId { get; set; }
        public Player Player { get; set; }

        public DateTime dateChanged { get; set; } = DateTime.Now;

        public IEnumerable<Player> Players { get; init; } = new List<Player>();
    }
}
