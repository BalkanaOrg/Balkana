using Microsoft.AspNetCore.Http;

namespace Balkana.Models.Admin
{
    public class BalkanaAwardsManualAwardViewModel
    {
        public int Year { get; set; }
        public DateTime AwardDate { get; set; } = DateTime.UtcNow.Date;

        public bool GiveEntryFragger { get; set; }
        public int? EntryFraggerPlayerId { get; set; }
        public IFormFile? EntryFraggerIcon { get; set; }

        public bool GiveAwper { get; set; }
        public int? AwperPlayerId { get; set; }
        public IFormFile? AwperIcon { get; set; }

        public bool GiveIgl { get; set; }
        public int? IglPlayerId { get; set; }
        public IFormFile? IglIcon { get; set; }

        public bool GiveTeamOfYear { get; set; }
        public int? TeamOfYearTeamId { get; set; }
        public IFormFile? TeamOfYearIcon { get; set; }

        public bool GiveTournamentOfYear { get; set; }
        public int? TournamentOfYearTournamentId { get; set; }
        public IFormFile? TournamentOfYearIcon { get; set; }

        public bool GiveContentCreator { get; set; }
        public string? ContentCreatorUserId { get; set; }
        public IFormFile? ContentCreatorIcon { get; set; }

        public bool GiveStreamer { get; set; }
        public string? StreamerUserId { get; set; }
        public IFormFile? StreamerIcon { get; set; }

        public bool GivePlayByPlayCaster { get; set; }
        public string? PlayByPlayCasterUserId { get; set; }
        public IFormFile? PlayByPlayCasterIcon { get; set; }

        public bool GiveColorCaster { get; set; }
        public string? ColorCasterUserId { get; set; }
        public IFormFile? ColorCasterIcon { get; set; }
    }
}

