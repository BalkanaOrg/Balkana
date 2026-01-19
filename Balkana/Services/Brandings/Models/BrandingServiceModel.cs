using Balkana.Data.Models;

namespace Balkana.Services.Brandings.Models
{
    public class BrandingServiceModel : IBrandingModel
    {
        public int Id { get; init; }
        public string FullName { get; init; }
        public string Tag { get; init; }
        public string LogoURL { get; init; }
        public int yearFounded { get; init; }
        public string? FounderId { get; init; }
        public string? ManagerId { get; init; }
        public string? FounderName { get; init; }
        public string? ManagerName { get; init; }
    }
}

