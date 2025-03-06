namespace Balkana.Models.Players
{
    using Balkana.Data.Models;
    using Balkana.Services.Players.Models;
    using static Data.DataConstants;
    public class AllPlayersQueryModel
    {
        public const int playersPerPage = 20;

        public string Nationality { get; set; }
        public IEnumerable<PlayerNationalityServiceModel> ManyNationalities { get; set; }

        public string SearchTerm { get; set; }

        public int CurrentPage { get; set; } = 1;

        public int TotalPlayers { get; set; }

        public IEnumerable<string> Nationalities { get; set; }
        public IEnumerable<PlayerServiceModel> Players { get; set; }
    }
}
