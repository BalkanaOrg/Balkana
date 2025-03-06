namespace Balkana.Services.Players.Models
{
    public interface IPlayerModel
    {
        string Nickname { get; }

        string FirstName { get; }
        
        string LastName { get; }
    }
}
