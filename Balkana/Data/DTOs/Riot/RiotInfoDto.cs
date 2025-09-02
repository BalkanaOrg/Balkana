namespace Balkana.Data.DTOs.Riot
{
    public class RiotInfoDto
    {
        public string endOfGameResult { get; set; }
        public long gameCreation { get; set; }
        public int gameDuration { get; set; }
        public long gameEndTimestamp { get; set; }
        public long gameId { get; set; }
        public string gameMode { get; set; }
        public string gameName { get; set; }
        public long gameStartTimestamp { get; set; }
        public string gameType { get; set; }
        public string gameVersion { get; set; }
        public int mapId { get; set; }

        public List<RiotParticipantDto> participants { get; set; }
        public List<RiotTeamDto> teams { get; set; }
    }
}
