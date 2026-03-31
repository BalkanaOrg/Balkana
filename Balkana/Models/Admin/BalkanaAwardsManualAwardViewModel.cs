using Microsoft.AspNetCore.Http;

namespace Balkana.Models.Admin
{
    public class BalkanaAwardsManualAwardViewModel
    {
        public int Year { get; set; }
        public DateTime AwardDate { get; set; } = DateTime.UtcNow.Date;

        // Counter-Strike POTY (manual)
        public bool GiveCsPoty1 { get; set; }
        public int? CsPotyPlayerId1 { get; set; }
        public IFormFile? CsPotyIcon1 { get; set; }
        public bool GiveCsPoty2 { get; set; }
        public int? CsPotyPlayerId2 { get; set; }
        public IFormFile? CsPotyIcon2 { get; set; }
        public bool GiveCsPoty3 { get; set; }
        public int? CsPotyPlayerId3 { get; set; }
        public IFormFile? CsPotyIcon3 { get; set; }
        public bool GiveCsPoty4 { get; set; }
        public int? CsPotyPlayerId4 { get; set; }
        public IFormFile? CsPotyIcon4 { get; set; }
        public bool GiveCsPoty5 { get; set; }
        public int? CsPotyPlayerId5 { get; set; }
        public IFormFile? CsPotyIcon5 { get; set; }
        public bool GiveCsPoty6 { get; set; }
        public int? CsPotyPlayerId6 { get; set; }
        public IFormFile? CsPotyIcon6 { get; set; }
        public bool GiveCsPoty7 { get; set; }
        public int? CsPotyPlayerId7 { get; set; }
        public IFormFile? CsPotyIcon7 { get; set; }
        public bool GiveCsPoty8 { get; set; }
        public int? CsPotyPlayerId8 { get; set; }
        public IFormFile? CsPotyIcon8 { get; set; }
        public bool GiveCsPoty9 { get; set; }
        public int? CsPotyPlayerId9 { get; set; }
        public IFormFile? CsPotyIcon9 { get; set; }
        public bool GiveCsPoty10 { get; set; }
        public int? CsPotyPlayerId10 { get; set; }
        public IFormFile? CsPotyIcon10 { get; set; }

        // League of Legends POTY (manual)
        public bool GiveLolPoty1 { get; set; }
        public int? LolPotyPlayerId1 { get; set; }
        public IFormFile? LolPotyIcon1 { get; set; }
        public bool GiveLolPoty2 { get; set; }
        public int? LolPotyPlayerId2 { get; set; }
        public IFormFile? LolPotyIcon2 { get; set; }
        public bool GiveLolPoty3 { get; set; }
        public int? LolPotyPlayerId3 { get; set; }
        public IFormFile? LolPotyIcon3 { get; set; }
        public bool GiveLolPoty4 { get; set; }
        public int? LolPotyPlayerId4 { get; set; }
        public IFormFile? LolPotyIcon4 { get; set; }
        public bool GiveLolPoty5 { get; set; }
        public int? LolPotyPlayerId5 { get; set; }
        public IFormFile? LolPotyIcon5 { get; set; }
        public bool GiveLolPoty6 { get; set; }
        public int? LolPotyPlayerId6 { get; set; }
        public IFormFile? LolPotyIcon6 { get; set; }
        public bool GiveLolPoty7 { get; set; }
        public int? LolPotyPlayerId7 { get; set; }
        public IFormFile? LolPotyIcon7 { get; set; }
        public bool GiveLolPoty8 { get; set; }
        public int? LolPotyPlayerId8 { get; set; }
        public IFormFile? LolPotyIcon8 { get; set; }
        public bool GiveLolPoty9 { get; set; }
        public int? LolPotyPlayerId9 { get; set; }
        public IFormFile? LolPotyIcon9 { get; set; }
        public bool GiveLolPoty10 { get; set; }
        public int? LolPotyPlayerId10 { get; set; }
        public IFormFile? LolPotyIcon10 { get; set; }

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

