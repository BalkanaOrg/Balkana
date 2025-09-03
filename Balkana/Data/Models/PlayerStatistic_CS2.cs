using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    public class PlayerStatistic_CS2 : PlayerStatistic
    {
        public int Damage { get; set; }
        public int TsideRoundsWon { get; set; }
        public int CTsideRoundsWon { get; set; }
        public int RoundsPlayed { get; set; }
        public int Kills { get; set; }
        public int Assists { get; set; }
        public int Deaths { get; set; }
        public int KAST { get; set; }
        public int HSkills { get; set; }
        public double HLTV3 { get; set; }
        public double HLTV2 { get; set; }
        public double HLTV1 { get; set; }
        public int UD { get; set; }
        public int FK { get; set; }
        public int FD { get; set; }
        public int TK { get; set; }
        public int TD { get; set; }
        public int WallbangKills { get; set; }
        public int CollateralKills { get; set; }
        public int NoScopeKills { get; set; }
        public int _5k { get; set; }
        public int _4k { get; set; }
        public int _3k { get; set; }
        public int _2k { get; set; }
        public int _1k { get; set; }
        public int _1v5 { get; set; } //ot tuka se maha
        public int _1v4 { get; set; }
        public int _1v3 { get; set; }
        public int _1v2 { get; set; }
        public int _1v1 { get; set; } //do tuka
        public int SniperKills { get; set; }
        public int PistolKills { get; set; }
        public int KnifeKills { get; set; }
        public int UtilityUsage { get; set; }
        public int Flashes { get; set; }
    }
}
