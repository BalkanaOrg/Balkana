namespace Balkana.Models.Tournaments
{
    public class RiotMatchDetailsViewModel
    {
        public string MatchId { get; set; } = "";
        public string GameVersion { get; set; } = "";
        public string DDragonVersion { get; set; } = "";
        public int GameDurationSeconds { get; set; }
        public string GameMode { get; set; } = "";
        public string GameType { get; set; } = "";
        public long GameStartTimestamp { get; set; }
        public int MapId { get; set; }

        public bool BlueTeamWon { get; set; }
        public List<RiotMatchParticipantViewModel> BlueTeam { get; set; } = new();
        public List<RiotMatchParticipantViewModel> RedTeam { get; set; } = new();
    }

    public class RiotMatchParticipantViewModel
    {
        public string Puuid { get; set; } = "";
        public string RiotId { get; set; } = "";  // gameName#tagLine
        public string ChampionName { get; set; } = "";
        public int ChampionId { get; set; }
        public string TeamPosition { get; set; } = "";
        public int ChampLevel { get; set; }

        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public double Kda => Deaths == 0 ? Kills + Assists : Math.Round((Kills + Assists) / (double)Deaths, 1);

        public int GoldEarned { get; set; }
        public int CreepScore { get; set; }
        public int VisionScore { get; set; }
        public int TotalDamageToChampions { get; set; }
        public int DamageToObjectives { get; set; }

        public int Item0 { get; set; }
        public int Item1 { get; set; }
        public int Item2 { get; set; }
        public int Item3 { get; set; }
        public int Item4 { get; set; }
        public int Item5 { get; set; }
        public int Item6 { get; set; }
        public int[] Items => new[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6 };

        public int Summoner1Id { get; set; }
        public int Summoner2Id { get; set; }

        /// <summary>Maps Riot summoner spell ID to DDragon image filename (no extension).</summary>
        public static string? GetSummonerSpellName(int id) => id switch
        {
            1 => "SummonerBoost", 3 => "SummonerExhaust", 4 => "SummonerFlash", 6 => "SummonerHaste",
            7 => "SummonerHeal", 11 => "SummonerSmite", 12 => "SummonerTeleport", 13 => "SummonerMana",
            14 => "SummonerDot", 21 => "SummonerBarrier", 32 => "SummonerSnowball", 39 => "SummonerSnowURFSnowball_Mark",
            30 => "SummonerPoroRecall", 31 => "SummonerPoroThrow", 54 => "Summoner_UltBookPlaceholder",
            55 => "Summoner_UltBookSmitePlaceholder", 2201 => "SummonerCherryHold", 2202 => "SummonerCherryFlash",
            _ => null
        };
    }

    public class RiotMatchParticipantRowVM
    {
        public RiotMatchParticipantViewModel Participant { get; set; } = null!;
        public string ChampImgBase { get; set; } = "";
        public string ItemImgBase { get; set; } = "";
        public string SpellImgBase { get; set; } = "";
    }
}
