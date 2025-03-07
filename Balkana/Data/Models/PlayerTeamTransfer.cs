using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models
{
    public class PlayerTeamTransfer
    {
        public int Id { get; set; }
        [Required]
        public int PlayerId { get; set; }
        [ForeignKey("PlayerId")]
        public Player Player { get; set; }
        public int TeamId { get; set; } = 0;
        [ForeignKey("TeamId")]
        public Team Team { get; set; }
        [Required]
        public DateTime TransferDate { get; set; }

        public int PositionId { get; set; }
        [ForeignKey("PositionId")]
        public TeamPosition TeamPosition { get; set; }

        public IEnumerable<Team> Teams { get; init; } = new List<Team>();
    }
}
