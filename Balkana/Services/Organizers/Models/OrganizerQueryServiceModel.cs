using Balkana.Services.Teams.Models;

namespace Balkana.Services.Organizers.Models
{
    public class OrganizerQueryServiceModel
    {
        public int CurrentPage { get; init; }

        public int OrgsPerPage { get; init; }

        public int TotalOrgs { get; init; }

        public IEnumerable<OrganizerServiceModel> Organizations { get; init; }

        public IEnumerable<OrganizerTournamentsServiceModel> Tournaments { get; init; }
    }
}
