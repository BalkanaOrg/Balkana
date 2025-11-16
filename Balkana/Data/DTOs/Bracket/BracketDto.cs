namespace Balkana.Data.DTOs.Bracket
{
    public class BracketDto
    {
        public string Bracket { get; set; } // "Upper", "Lower", "GrandFinal"
        public int Round { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        public string TeamA { get; set; }
        public string TeamB { get; set; }
        public string Next { get; set; }
        public string Winner { get; set; }
    }
}
