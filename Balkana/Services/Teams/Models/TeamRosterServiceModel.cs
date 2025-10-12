using Balkana.Data.Models;

namespace Balkana.Services.Teams.Models
{
    public class TeamRosterServiceModel
    {
        public int PlayerId { get; set; }
        public string Nickname { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PictureUrl { get; set; }
        public int? PositionId { get; set; }
        public string PositionName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public PlayerTeamStatus Status { get; set; }
        public bool IsCurrentRoster { get; set; }
    }
}
