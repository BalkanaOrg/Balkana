namespace Balkana.Services.Players.Models
{
    public class PlayerPictureServiceModel
    {
        public int Id { get; init; }
        public int PlayerId { get; init; }
        public string PictureURL { get; init; }
        public DateTime DateChanged { get; init; }
    }
}
