namespace Balkana.Models.Admin
{
    public class PlayerWithProfilesViewModel
    {
        public int PlayerId { get; set; }
        public string Nickname { get; set; } = "";
        public string FullName { get; set; } = "";
        public List<GameProfileAdminRow> ProfileRows { get; set; } = new();
    }
}
