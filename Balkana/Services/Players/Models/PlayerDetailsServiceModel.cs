namespace Balkana.Services.Players.Models
{
    public class PlayerDetailsServiceModel : PlayerServiceModel
    {
        // ✅ Player has many pictures, but we’ll mainly show the latest
        public IEnumerable<PlayerPictureServiceModel> PlayerPictures { get; set; }
            = new List<PlayerPictureServiceModel>();

        // ✅ Nationality info
        public string NationalityName { get; set; }
        public string FlagUrl { get; set; }
    }
}
