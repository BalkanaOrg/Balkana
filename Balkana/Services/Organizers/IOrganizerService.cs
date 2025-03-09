using Balkana.Services.Organizers.Models;

namespace Balkana.Services.Organizers
{
    public interface IOrganizerService
    {
        public OrganizerQueryServiceModel All(
            string searchTerm = null,
            int currentPage = 1,
            int organizersPerPage = int.MaxValue
            );

        int Create(
            string FullName,
            string Tag,
            string Description,
            string LogoURL
            );

        bool Edit(
            int id,
            string FullName,
            string Tag,
            string Description,
            string LogoURL
            );

        OrganizerDetailsServiceModel Details(int id);

        public IEnumerable<string> GetAllTournaments();
        public IEnumerable<OrganizerTournamentsServiceModel> AllTournaments();
    }
}
