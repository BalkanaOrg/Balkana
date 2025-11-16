using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Tournaments
{
    public class TournamentTeamsViewModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }

        [Display(Name = "Select Teams")]
        public List<int> SelectedTeamIds { get; set; } = new List<int>();

        public List<TeamSelectItem> AvailableTeams { get; set; } = new List<TeamSelectItem>();
    }
}
