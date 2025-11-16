using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Tournaments
{
    public class TrophyService
    {
        private readonly ApplicationDbContext _context;

        public TrophyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AwardChampionTrophyAsync(int tournamentId, int teamId, string trophyDescription)
        {
            // Get tournament to use its end date
            var tournament = await _context.Tournaments
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            // Create tournament trophy
            var tournamentTrophy = new TrophyTournament
            {
                Name = $"Champion of {tournament.FullName}",
                TournamentId = tournamentId,
                Description = trophyDescription,
                IconURL = "/uploads/Tournaments/Trophies/default_trophy.png", // Default champion icon
                AwardType = "Trophy",
                AwardDate = tournament?.EndDate ?? DateTime.UtcNow
            };

            _context.Trophies.Add(tournamentTrophy);
            await _context.SaveChangesAsync();

            // Award trophy to team
            var teamTrophy = new TeamTrophy
            {
                TeamId = teamId,
                TrophyId = tournamentTrophy.Id
            };

            _context.TeamTrophies.Add(teamTrophy);

            // Award trophy to all team members who played in the tournament
            // Reuse the tournament query from above

            if (tournament != null)
            {
                // Find players who were on the team during the tournament period
                var teamMembers = await _context.PlayerTeamTransfers
                    .Where(tr => tr.TeamId == teamId &&
                                tr.Status == PlayerTeamStatus.Active &&
                                tr.StartDate <= tournament.EndDate && // Player joined before tournament ended
                                (tr.EndDate == null || tr.EndDate >= tournament.StartDate)) // Player still on team during tournament
                    .Select(tr => tr.PlayerId)
                    .ToListAsync();

                Console.WriteLine($"🏆 Awarding champion trophy to {teamMembers.Count} team members of team {teamId} for tournament {tournamentId}");

                foreach (var playerId in teamMembers)
                {
                    var playerTrophy = new PlayerTrophy
                    {
                        PlayerId = playerId,
                        TrophyId = tournamentTrophy.Id
                    };

                    _context.PlayerTrophies.Add(playerTrophy);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task AwardPlayerTrophyAsync(int playerId, string awardType, string description, int tournamentId)
        {
            // Get tournament to use its end date
            var tournament = await _context.Tournaments
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            // Create tournament trophy
            var tournamentTrophy = new TrophyTournament
            {
                Name = $"Champion of {tournament.FullName}",
                TournamentId = tournamentId,
                Description = description,
                IconURL = GetTrophyIconForAwardType(awardType),
                AwardType = awardType,
                AwardDate = tournament?.EndDate ?? DateTime.UtcNow
            };

            _context.Trophies.Add(tournamentTrophy);
            await _context.SaveChangesAsync();

            // Award trophy to player
            var playerTrophy = new PlayerTrophy
            {
                PlayerId = playerId,
                TrophyId = tournamentTrophy.Id
            };

            _context.PlayerTrophies.Add(playerTrophy);
            await _context.SaveChangesAsync();
        }

        public async Task AwardMultiplePlayerTrophiesAsync(List<int> playerIds, string awardType, string description, int tournamentId)
        {
            if (!playerIds.Any()) return;

            // Get tournament to use its end date
            var tournament = await _context.Tournaments
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            // Create a single trophy that will be awarded to multiple players
            var tournamentTrophy = new TrophyTournament
            {
                Name = $"Champion of {tournament.FullName}",
                TournamentId = tournamentId,
                Description = description,
                IconURL = GetTrophyIconForAwardType(awardType),
                AwardType = awardType,
                AwardDate = tournament?.EndDate ?? DateTime.UtcNow
            };

            _context.Trophies.Add(tournamentTrophy);
            await _context.SaveChangesAsync();

            // Award the same trophy to all selected players
            foreach (var playerId in playerIds)
            {
                var playerTrophy = new PlayerTrophy
                {
                    PlayerId = playerId,
                    TrophyId = tournamentTrophy.Id
                };

                _context.PlayerTrophies.Add(playerTrophy);
            }

            await _context.SaveChangesAsync();
        }

        private string GetTrophyIconForAwardType(string awardType)
        {
            return awardType.ToLower() switch
            {
                "mvp" => "/uploads/Tournaments/MVP.png",
                "evp" => "/uploads/Tournaments/EVP.png",
                "champion" => "/uploads/Tournaments/Champion.png",
                _ => "/uploads/Tournaments/Default.png"
            };
        }

        public async Task<List<Trophy>> GetPlayerTrophiesAsync(int playerId)
        {
            return await _context.PlayerTrophies
                .Include(pt => pt.Trophy)
                .Where(pt => pt.PlayerId == playerId)
                .Select(pt => pt.Trophy)
                .OrderByDescending(t => t.AwardDate)
                .ToListAsync();
        }

        public async Task<List<Trophy>> GetTeamTrophiesAsync(int teamId)
        {
            return await _context.TeamTrophies
                .Include(tt => tt.Trophy)
                .Where(tt => tt.TeamId == teamId)
                .Select(tt => tt.Trophy)
                .OrderByDescending(t => t.AwardDate)
                .ToListAsync();
        }
    }
}
