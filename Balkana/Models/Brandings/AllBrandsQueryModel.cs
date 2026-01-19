namespace Balkana.Models.Brandings
{
    using Balkana.Services.Brandings.Models;

    public class AllBrandsQueryModel
    {
        public string SearchTerm { get; set; }

        public const int BrandingsPerPage = 20;

        public int CurrentPage { get; set; } = 1;

        public int TotalBrandings { get; set; }

        public int AbsoluteNumberOfBrandings { get; set; } = 0;

        public IEnumerable<BrandingServiceModel> Brandings { get; set; }
    }
}

