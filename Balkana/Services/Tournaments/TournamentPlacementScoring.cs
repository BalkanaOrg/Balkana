using System.Text.Json;
using Balkana.Data.Models;

namespace Balkana.Services.Tournaments
{
    public static class TournamentPlacementScoring
    {
        public static int GetPointsForPlacement(Tournament tournament, int placement)
        {
            if (!string.IsNullOrEmpty(tournament.PointsConfiguration))
            {
                try
                {
                    var pointsConfig = JsonSerializer.Deserialize<Dictionary<string, int>>(tournament.PointsConfiguration);
                    if (pointsConfig != null && pointsConfig.TryGetValue(placement.ToString(), out var pts))
                        return pts;
                }
                catch
                {
                    // Fall through to defaults
                }
            }

            return placement switch
            {
                1 => 500,
                2 => 325,
                3 => 200,
                4 => 125,
                5 => 100,
                6 => 75,
                7 => 50,
                8 => 25,
                9 => 20,
                10 => 15,
                11 => 12,
                12 => 10,
                13 => 8,
                14 => 6,
                15 => 4,
                16 => 2,
                _ => Math.Max(1, 20 - placement)
            };
        }
    }
}
