namespace Balkana.Services.Players.Models
{
    public class PlayerPictureServiceModel
    {
        public int Id { get; init; }
        public int PlayerId { get; init; }
        public string PictureUrl { get; init; }
        public DateTime DateChanged { get; init; }
    }
}
