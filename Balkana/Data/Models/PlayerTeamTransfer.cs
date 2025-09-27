using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Data.Models
{
    public class PlayerTeamTransfer
    {
        public int Id { get; set; }

        [Required]
        public int PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }

        public int? TeamId { get; set; } // null if Free Agent / Retired
        [ForeignKey(nameof(TeamId))]
        public Team Team { get; set; }

        [Required]
        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime? EndDate { get; set; } // null means still valid

        [Required]
        public PlayerTeamStatus Status { get; set; } = PlayerTeamStatus.Active;

        public int? PositionId { get; set; }
        [ForeignKey(nameof(PositionId))]
        public TeamPosition TeamPosition { get; set; }
    }
}
