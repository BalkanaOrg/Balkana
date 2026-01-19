namespace Balkana.Services.Brandings.Models
{
    public class BrandingQueryServiceModel
    {
        public int CurrentPage { get; init; }
        public int BrandingsPerPage { get; init; }
        public int TotalBrandings { get; init; }
        public IEnumerable<BrandingServiceModel> Brandings { get; init; }
    }
}

