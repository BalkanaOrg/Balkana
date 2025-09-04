namespace Balkana.Services.Teams.Models
{
    public class TeamDetailsServiceModel : TeamServiceModel
    {
        public string Description { get; set; }

        public int GameId { get; init; }

        public IEnumerable<TeamStaffServiceModel> Players { get; set; }
    }
}
