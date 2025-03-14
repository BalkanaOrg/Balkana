﻿namespace Balkana.Services.Teams.Models
{
    public class TeamServiceModel : ITeamModel
    {
        public int Id { get; init; }
        public string FullName { get; init; }
        public string Tag { get; init; }
        public string LogoURL { get; init; }
        public int YearFounded { get; init; }
        public string GameName { get; init; }
    }
}
