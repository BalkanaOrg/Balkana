namespace Balkana.Data.DTOs.Riot
{
    public class RiotParticipantDto
    {
        public string puuid { get; set; }
        public string summonerId { get; set; }
        public string riotIdGameName { get; set; }
        public string riotIdTagline { get; set; }

        public int participantId { get; set; }
        public int teamId { get; set; }

        public int championId { get; set; }
        public string championName { get; set; }
        public string teamPosition { get; set; }

        // Performance
        public int kills { get; set; }
        public int deaths { get; set; }
        public int assists { get; set; }

        public int goldEarned { get; set; }
        public int totalMinionsKilled { get; set; }
        public int neutralMinionsKilled { get; set; }
        public int visionScore { get; set; }
        public int totalDamageDealtToChampions { get; set; }

        // Items
        public int item0 { get; set; }
        public int item1 { get; set; }
        public int item2 { get; set; }
        public int item3 { get; set; }
        public int item4 { get; set; }
        public int item5 { get; set; }
        public int item6 { get; set; }

        // Spells
        public int summoner1Id { get; set; }
        public int summoner2Id { get; set; }

        // Extra info if needed
        public int champLevel { get; set; }
        public int damageDealtToObjectives { get; set; }
        public int damageDealtToTurrets { get; set; }
    }
}
