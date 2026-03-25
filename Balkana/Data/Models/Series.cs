using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Balkana.Data.Models
{
    public class Series
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public int? TeamAId { get; set; }
        public int? TeamBId { get; set; }
        public Team? TeamA { get; set; }
        public Team? TeamB { get; set; }

        // Winner information
        public int? WinnerTeamId { get; set; }
        public Team? WinnerTeam { get; set; }

        public DateTime DatePlayed { get; set; }

        public bool isFinished { get; set; } = false;

        public ICollection<Match> Matches { get; set; } = new List<Match>();

        // New fields for bracket logic
        public int Round { get; set; }              // 1 = Quarterfinal, 2 = Semifinal, 3 = Final, etc.
        public int Position { get; set; }           // Position in the round (helps ordering)
        public int? BestOf { get; set; }          // 1, 3, 5 (Bo1/Bo3/Bo5) - used for Riot best-of winner logic (nullable for Faceit)

        // Optional: reference the "next" series this feeds into
        [AllowNull]
        public int? NextSeriesId { get; set; }
        public Series NextSeries { get; set; }

        public BracketType Bracket { get; set; }
    }
}
