namespace Balkana.Services.Organizers.Models
{
    public class OrganizerServiceModel : IOrganizerModel
    {
        public int Id { get; init; }
        public string FullName { get; init; }
        public string Tag { get; init; }
        public string Description { get; init; }
        public string LogoURL { get; init; }
    }
}
