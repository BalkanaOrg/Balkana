using Balkana.Services.Players.Models;
using Balkana.Services.Teams.Models;

namespace Balkana.Data.Infrastructure.Extensions
{
    public static class ModelExtensions
    {
        public static string GetInformation(this ITeamModel team)
            => team.Tag + "-" + team.FullName;

        public static string GetInformation(this IPlayerModel player)
            => player.FirstName + "-" + player.Nickname + "-" + player.LastName;
    }
}
