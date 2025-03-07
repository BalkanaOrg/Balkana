using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    public class TeamPosition
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Icon { get; set; }

        public ICollection<PlayerTeamTransfer> Transfers { get; set; }
    }
}
