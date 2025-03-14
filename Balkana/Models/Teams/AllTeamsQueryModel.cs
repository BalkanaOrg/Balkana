﻿namespace Balkana.Models.Teams
{
    using Balkana.Data.Models;
    using Balkana.Services.Teams.Models;
    using static Data.DataConstants;
    public class AllTeamsQueryModel
    {
        public const int TeamsPerPage = 20;

        public string Game { get; set; }
        public IEnumerable<TeamGameServiceModel> ManyGames { get; set; }

        public string SearchTerm { get; set; }

        public int CurrentPage { get; set; } = 1;

        public int TotalTeams { get; set; }

        public IEnumerable<string> Games { get; set; }
        public IEnumerable<TeamServiceModel> Teams { get; set; }
    }
}
