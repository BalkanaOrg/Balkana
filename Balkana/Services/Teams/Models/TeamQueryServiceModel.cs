namespace Balkana.Services.Teams.Models
{
    public class TeamQueryServiceModel
    {
        public int CurrentPage { get; init; }

        public int TeamsPerPage { get; init; }

        public int TotalTeams { get; init; }

        public IEnumerable<TeamServiceModel> Teams { get; init; }

        public IEnumerable<TeamStaffServiceModel> StaffMembers { get; init; }
    }
}
