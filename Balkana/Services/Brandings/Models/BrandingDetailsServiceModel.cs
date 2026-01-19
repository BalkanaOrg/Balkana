using Balkana.Services.Teams.Models;

namespace Balkana.Services.Brandings.Models
{
    public class BrandingDetailsServiceModel : BrandingServiceModel
    {
        public IEnumerable<TeamServiceModel> Teams { get; set; } = new List<TeamServiceModel>();
    }
}

