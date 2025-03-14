﻿namespace Balkana.Services.Players.Models
{
    public class PlayerQueryServiceModel
    {
        public int CurrentPage { get; init; }
        public int PlayersPerPage { get; init; }
        public int TotalPlayers { get; init; }

        public IEnumerable<PlayerServiceModel> Players { get; init; }
        public IEnumerable<PlayerPictureServiceModel> PlayerPictures { get; init; }
    }
}
