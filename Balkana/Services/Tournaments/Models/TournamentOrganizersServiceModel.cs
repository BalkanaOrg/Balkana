using System.ComponentModel.DataAnnotations;

namespace Balkana.Services.Tournaments.Models
{
    public class TournamentOrganizersServiceModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Tag { get; set; }
        public string Description { get; set; }
        public string LogoURL { get; set; }
    }
}
