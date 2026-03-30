using System.Collections.Generic;
using System.Text.Json;
using Balkana.Data.Models;

namespace Balkana.Services.Tournaments
{
    public static class TournamentPrizeDistribution
    {
        /// <summary>
        /// Prize money for a single placement from JSON keys "1","2",... as fractions of <see cref="Tournament.PrizePool"/>,
        /// or the built-in default ladder if JSON is missing/invalid.
        /// </summary>
        public static decimal GetPrizeForPlacement(Tournament tournament, int placement)
        {
            if (!string.IsNullOrEmpty(tournament.PrizeConfiguration))
            {
                try
                {
                    var prizeConfig = JsonSerializer.Deserialize<Dictionary<string, decimal>>(tournament.PrizeConfiguration);
                    if (prizeConfig != null && prizeConfig.TryGetValue(placement.ToString(), out var fraction))
                        return tournament.PrizePool * fraction;
                }
                catch
                {
                    // Fall through to defaults
                }
            }

            if (tournament.PrizePool > 0)
            {
                return placement switch
                {
                    1 => tournament.PrizePool * 0.50m,
                    2 => tournament.PrizePool * 0.30m,
                    3 => tournament.PrizePool * 0.20m,
                    4 => tournament.PrizePool * 0.10m,
                    5 => tournament.PrizePool * 0.05m,
                    6 => tournament.PrizePool * 0.03m,
                    7 => tournament.PrizePool * 0.02m,
                    8 => tournament.PrizePool * 0.01m,
                    _ => 0
                };
            }

            return 0;
        }
    }
}
