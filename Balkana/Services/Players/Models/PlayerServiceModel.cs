namespace Balkana.Services.Players.Models
{
    public class PlayerServiceModel : IPlayerModel
    {
        public int Id { get; init; }
        public string Nickname { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
        public int NationalityId { get; init; }

        public string PictureUrl { get; set; }
    }
}
