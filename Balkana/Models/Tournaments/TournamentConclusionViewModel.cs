using System.ComponentModel.DataAnnotations;

namespace Balkana.Models.Tournaments
{
    public class TournamentConclusionViewModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public int TotalTeams { get; set; }
        public string EliminationType { get; set; }

        // MVP Selection
        public MVPSourceType MVPSourceType { get; set; } = MVPSourceType.Formula;
        public int? SelectedMVPId { get; set; }
        public List<PlayerMVPCandidate> MVPCandidates { get; set; } = new List<PlayerMVPCandidate>();
        public MVFormulaConfiguration MVFormulaConfig { get; set; } = new MVFormulaConfiguration();

        // EVP Selection
        public EVPSourceType EVPSourceType { get; set; } = EVPSourceType.Formula;
        public List<int> SelectedEVPIds { get; set; } = new List<int>();
        public List<PlayerEVPCandidate> EVPCandidates { get; set; } = new List<PlayerEVPCandidate>();

        // Trophy Information
        public bool AwardChampionTrophy { get; set; } = true;
        public string ChampionTrophyDescription { get; set; }
    }

    public enum MVPSourceType
    {
        Formula,
        Manual
    }

    public enum EVPSourceType
    {
        Formula,
        Manual
    }

    public class PlayerMVPCandidate
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string TeamName { get; set; }
        public int TeamId { get; set; }
        public bool IsInEligibleMatches { get; set; }
        public MVFormulaScore FormulaScore { get; set; }
    }

    public class PlayerEVPCandidate
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string TeamName { get; set; }
        public int TeamId { get; set; }
        public bool IsInEligibleMatches { get; set; }
        public MVFormulaScore FormulaScore { get; set; }
    }

    public class MVFormulaScore
    {
        public double TotalScore { get; set; }
        public double HLTVRatingScore { get; set; }
        public double KillPerRoundScore { get; set; }
        public double DamagePerRoundScore { get; set; }
        public double DeathsPerRoundScore { get; set; }
        public double UtilityDamageScore { get; set; }
        public double AWPkillsScore { get; set; }
        public double FirstKillsScore { get; set; }
        public double Commentator1Score { get; set; }
        public double Commentator2Score { get; set; }
    }

    public class MVFormulaConfiguration
    {
        // Points for each category (configurable)
        public int HLTVRatingPoints { get; set; } = 5;
        public int KillPerRoundPoints { get; set; } = 5;
        public int DamagePerRoundPoints { get; set; } = 2;
        public int DeathsPerRoundPoints { get; set; } = 2;
        public int UtilityDamagePoints { get; set; } = 3;
        public int AWPkillsPoints { get; set; } = 2;
        public int FirstKillsPoints { get; set; } = 3;
        public int Commentator1Points { get; set; } = 5;
        public int Commentator2Points { get; set; } = 5;

        // Commentator selections
        public int? Commentator1SelectedPlayerId { get; set; }
        public int? Commentator2SelectedPlayerId { get; set; }
        public string Commentator1Name { get; set; } = "Commentator A";
        public string Commentator2Name { get; set; } = "Commentator B";
    }
}
