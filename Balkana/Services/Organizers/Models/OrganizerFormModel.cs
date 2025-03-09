using Balkana.Services.Teams.Models;

namespace Balkana.Services.Organizers.Models
{
    public class OrganizerFormModel : ITeamModel
    {
        public string FullName { get; init; }
        public string Tag { get; init; }
        public string Description { get; init; }
        public string LogoURL { get; init; }
    }
}
