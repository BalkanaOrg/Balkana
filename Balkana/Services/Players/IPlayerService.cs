using Balkana.Models.Players;
using Balkana.Services.Players.Models;
using Balkana.Services.Teams.Models;

namespace Balkana.Services.Players
{
    public interface IPlayerService
    {
        public PlayerQueryServiceModel All(
            string searchTerm = null,
            int currentPage = 1,
            int playersPerPage = int.MaxValue
            );

        int Create(
            string nickname,
            string firstname,
            string lastname,
            int nationalityId);

        bool Edit(
            int id,
            string nickname,
            string firstname,
            string lastname,
            int nationalityId);

        PlayerDetailsServiceModel Profile(int playerId);

        public IEnumerable<PlayerNationalityServiceModel> GetNationalities();

        bool PictureExists(int pictureId);

        public IEnumerable<PlayerPictureServiceModel> AllPlayerPictures(int playerId);
        
    }
}
