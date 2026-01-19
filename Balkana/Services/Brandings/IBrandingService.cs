using Balkana.Services.Brandings.Models;

namespace Balkana.Services.Brandings
{
    public interface IBrandingService
    {
        BrandingQueryServiceModel All(
            string searchTerm = null,
            int currentPage = 1,
            int brandingsPerPage = int.MaxValue
        );

        int Create(
            string fullName,
            string tag,
            string logoUrl,
            int yearFounded,
            string? founderId = null,
            string? managerId = null
        );

        BrandingDetailsServiceModel Details(int id);

        int AbsoluteNumberOfBrandings();
    }
}

