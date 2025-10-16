using Balkana.Data.Models;
using Balkana.Services.Organizers.Models;
using Balkana.Services.Players.Models;
using Balkana.Services.Teams.Models;

namespace Balkana.Data.Infrastructure.Extensions
{
    public static class ModelExtensions
    {
        public static string GetInformation(this ITeamModel team)
            => team.Tag + "-" + team.FullName;

        public static string GetInformation(this IPlayerModel player)
            => player.Nickname;

        public static string GetInformation(this IOrganizerModel org)
            => org.Tag + "-" + org.FullName;

        public static string GetInformation(this Team team)
            => team.Tag + "-" + team.FullName;

        public static string GetInformation(this Player player)
            => player.Nickname;

        public static string GetInformation(this Organizer org)
            => org.Tag + "-" + org.FullName;
    }
}
