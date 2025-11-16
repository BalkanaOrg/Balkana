namespace Balkana.Services.Teams.Models
{
    public class TeamTrophyServiceModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string IconURL { get; set; }
        public string AwardType { get; set; }
        public DateTime AwardDate { get; set; }
    }
}