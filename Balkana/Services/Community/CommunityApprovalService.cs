using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Community
{
    public class CommunityApprovalService
    {
        private readonly ApplicationDbContext _db;

        public CommunityApprovalService(ApplicationDbContext db)
        {
            _db = db;
        }

        public string? GetRequiredLinkedAccountType(Game? game)
        {
            if (game == null) return null;

            var shortName = (game.ShortName ?? "").Trim();
            var fullName = (game.FullName ?? "").Trim();
            var normalized = (shortName + " " + fullName).ToUpperInvariant();

            if (normalized.Contains("CS"))
            {
                return "FaceIt";
            }

            if (normalized.Contains("LOL") || normalized.Contains("LEAGUE") || normalized.Contains("VALORANT") || normalized.Contains("VAL"))
            {
                return "Riot";
            }

            return null;
        }

        public async Task<int?> EnsurePlayerLinkedAsync(string userId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            if (user.PlayerId != null)
            {
                await tx.CommitAsync();
                return user.PlayerId;
            }

            var player = new Player
            {
                Nickname = user.UserName,
                FirstName = string.IsNullOrWhiteSpace(user.FirstName) ? user.UserName : user.FirstName,
                LastName = user.LastName ?? "",
                NationalityId = user.NationalityId != 0 ? user.NationalityId : 1
            };

            _db.Players.Add(player);
            await _db.SaveChangesAsync();

            user.PlayerId = player.Id;
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return player.Id;
        }

        public async Task<UserLinkedAccount?> GetUserLinkedAccountAsync(string userId, string type)
        {
            return await _db.UserLinkedAccounts.FirstOrDefaultAsync(ula => ula.UserId == userId && ula.Type == type);
        }
    }
}

