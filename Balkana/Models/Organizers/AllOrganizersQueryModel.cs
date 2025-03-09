using Balkana.Services.Organizers.Models;

namespace Balkana.Models.Organizers
{
    public class AllOrganizersQueryModel
    {
        public const int OrganizersPerPage = 25;
        public string SearchTerm { get; init; }
        public int CurrentPage { get; init; } = 1;
        public int TotalOrgs { get; set; }

        public IEnumerable<OrganizerServiceModel > Organizers { get; set; }
    }
}
