namespace Balkana.Services.Teams.Models
{
    public class TeamStaffServiceModel
    {
        public int Id { get; init; }
        public int PlayerId { get; init; }
        public string Nickname { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
        public int TeamId { get; init; }
        public int PictureId { get; init; }
        public int PositionId { get; init; }
        public int NationalityId { get; init; }
        public string PictureUrl { get; set; }
    }
}
