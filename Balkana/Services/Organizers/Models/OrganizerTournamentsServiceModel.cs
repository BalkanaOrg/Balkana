using Balkana.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Services.Organizers.Models
{
    public class OrganizerTournamentsServiceModel
    {
        public int Id { get; init; }
        public string FullName { get; init; }
        public string ShortName { get; init; }
        public int OrganizerId { get; init; }
        public string Description { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }

        //public IEnumerable series ?
}
}
