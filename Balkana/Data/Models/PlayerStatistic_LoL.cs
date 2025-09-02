namespace Balkana.Data.Models
{
    public class PlayerStatistic_LoL : PlayerStatistic
    {
        public int? Kills { get; set; }
        public int? Assists { get; set; }
        public int? Deaths { get; set; }

        public int ChampionId { get; set; }
        public string ChampionName { get; set; }
        public string Lane { get; set; }

        public int GoldEarned { get; set; }
        public int CreepScore { get; set; }
        public int VisionScore { get; set; }

        //Damage
        public int? TotalDamageToChampions { get; set; }
        public int? TotalDamageToObjectives { get; set; }

        // Items
        public int Item0 { get; set; }
        public int Item1 { get; set; }
        public int Item2 { get; set; }
        public int Item3 { get; set; }
        public int Item4 { get; set; }
        public int Item5 { get; set; }
        public int Item6 { get; set; }
    }
}
