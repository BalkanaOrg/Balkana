using AutoMapper;
using Balkana.Data.Models;
using Balkana.Data.Services.Maps;
using Balkana.Models.Players;
using Balkana.Services.Players.Models;
using Balkana.Services.Teams.Models;

namespace Balkana.Data.Infrastructure
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            this.CreateMap<csMap, csMapServiceModel>();


            //Teams
            this.CreateMap<Game, TeamGameServiceModel>();
            this.CreateMap<Player, TeamStaffServiceModel>();

            this.CreateMap<TeamDetailsServiceModel, TeamFormModel>();

            this.CreateMap<Team, TeamServiceModel>()
                .ForMember(c => c.FullName, cfg => cfg.MapFrom(c => c.Game.FullName));
            this.CreateMap<PlayerTeamTransfer, TeamStaffServiceModel>()
                .ForMember(c => c.TeamId, cfg => cfg.MapFrom(c => c.TeamId));
            this.CreateMap<Player, PlayerTeamTransfer>()
                .ForMember(c => c.PlayerId, cfg => cfg.MapFrom(c => c.Id));
            this.CreateMap<Team, TeamDetailsServiceModel>()
                .ForMember(c => c.FullName, cfg => cfg.MapFrom(c => c.Game.FullName));

            this.CreateMap<Player, PlayerServiceModel>()
                .ForMember(c=>c.Id, cfg=>cfg.MapFrom(cfg => cfg.Id));
            this.CreateMap<Player, PlayerDetailsServiceModel>()
                .ForMember(c=>c.Id, cfg=>cfg.MapFrom(cfg=>cfg.Id));
            this.CreateMap<Nationality, PlayerNationalityServiceModel>()
                .ForMember(c => c.Id, cfg => cfg.MapFrom(cfg => cfg.Id));
            this.CreateMap<PlayerPicture, PlayerPictureServiceModel>();
        }
    }
}
