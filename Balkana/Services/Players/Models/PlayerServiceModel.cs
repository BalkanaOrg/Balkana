namespace Balkana.Services.Players.Models
{
    public class PlayerServiceModel : IPlayerModel
    {
        public int Id { get; init; }
        public string Nickname { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
        public int NationalityId { get; init; }
        public DateTime BirthDate { get; init; }

        public string PictureUrl { get; set; }

        public List<PlayerTrophyServiceModel> Trophies { get; set; } = new();
        public List<PlayerTrophyServiceModel> MVPs { get; set; } = new();
        public List<PlayerTrophyServiceModel> EVPs { get; set; } = new();
    }
}
