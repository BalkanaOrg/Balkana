﻿using AutoMapper;
using Balkana.Data.Models;
using Balkana.Data.Services.Maps;
using Balkana.Models.Players;
using Balkana.Services.Organizers.Models;
using Balkana.Services.Players.Models;
using Balkana.Services.Teams.Models;
using Balkana.Services.Transfers.Models;

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

            //Players
            this.CreateMap<Player, PlayerServiceModel>()
                .ForMember(c=>c.Id, cfg=>cfg.MapFrom(cfg => cfg.Id));
            this.CreateMap<Player, PlayerDetailsServiceModel>()
                .ForMember(c=>c.Id, cfg=>cfg.MapFrom(cfg=>cfg.Id));
            this.CreateMap<Nationality, PlayerNationalityServiceModel>()
                .ForMember(c => c.Id, cfg => cfg.MapFrom(cfg => cfg.Id));
            this.CreateMap<PlayerPicture, PlayerPictureServiceModel>();

            //Transfers
            this.CreateMap<Player, TransferPlayersServiceModel>()
                .ForMember(c => c.Id, cfg => cfg.MapFrom(cfg => cfg.Id))
                .ForMember(c => c.Nickname, cfg => cfg.MapFrom(cfg => cfg.Nickname));
            this.CreateMap<Team, TransferTeamsServiceModel>()
                .ForMember(c=>c.Id, cfg=> cfg.MapFrom(cfg => cfg.Id));
            this.CreateMap<PlayerTeamTransfer, TransfersServiceModel>()
                .ForMember(c => c.Id, cfg => cfg.MapFrom(cfg => cfg.Id));
            this.CreateMap<TeamPosition, TransferPositionsServiceModel>()
                .ForMember(c => c.Id, cfg => cfg.MapFrom(cfg => cfg.Id));
            
            this.CreateMap<PlayerTeamTransfer, TransferPlayersServiceModel>()
                .ForMember(c => c.Id, cfg => cfg.MapFrom(c => c.Player.Id))
                .ForMember(c => c.Nickname, cfg => cfg.MapFrom(c => c.Player.Nickname));
            this.CreateMap<TransfersServiceModel, TransferDetailsServiceModel>();
            this.CreateMap<PlayerTeamTransfer, TransferDetailsServiceModel>(); 


            //TransfersServiceModel
            this.CreateMap<Player, TransfersServiceModel>()
                .ForMember(c=>c.PlayerId, cfg=>cfg.MapFrom(cfg=>cfg.Id))
                .ForMember(c=>c.PlayerUsername, cfg=>cfg.MapFrom(cfg=>cfg.Nickname));
            this.CreateMap<Team, TransfersServiceModel>()
                .ForMember(c=>c.TeamFullName, cfg=>cfg.MapFrom(cfg=>cfg.FullName))
                .ForMember(c=>c.TeamId, cfg=>cfg.MapFrom(cfg=>cfg.Id));
            this.CreateMap<TeamPosition, TransfersServiceModel>()
                .ForMember(c=>c.Position, cfg=>cfg.MapFrom(cfg=>cfg.Name));
            this.CreateMap<Game, TransfersServiceModel>()
                .ForMember(c=>c.GameName, cfg=>cfg.MapFrom(cfg=>cfg.IconURL));

            this.CreateMap<PlayerTeamTransfer, TransfersServiceModel>()
                .ForMember(c => c.Id, cfg => cfg.MapFrom(src => src.Id))
                .ForMember(c => c.GameName, cfg => cfg.MapFrom(src => src.Team.Game.IconURL)) // Ensure navigation property exists
                .ForMember(c => c.PlayerUsername, cfg => cfg.MapFrom(src => src.Player.Nickname))
                .ForMember(c => c.TeamFullName, cfg => cfg.MapFrom(src => src.Team.FullName))
                .ForMember(c => c.TransferDate, cfg => cfg.MapFrom(src => src.TransferDate))
                .ForMember(c => c.Position, cfg => cfg.MapFrom(src => src.TeamPosition.Name));
            this.CreateMap<Player, TransferDetailsServiceModel>()
                .ForMember(c=>c.PlayerId, cfg=>cfg.MapFrom(src=>src.Id));
            this.CreateMap<Team, TransferDetailsServiceModel>()
                .ForMember(c=>c.TeamId, cfg=>cfg.MapFrom(src=>src.Id));
            this.CreateMap<TeamPosition, TransferDetailsServiceModel>()
                .ForMember(c=>c.Position, cfg=>cfg.MapFrom(src=>src.Name));
            this.CreateMap<Game, TransferDetailsServiceModel>()
                .ForMember(c=>c.GameName, cfg=>cfg.MapFrom(src=>src.IconURL));

            //Organizers
            this.CreateMap<Organizer, OrganizerServiceModel>()
                .ForMember(c => c.Id, cfg => cfg.MapFrom(cfg => cfg.Id))
                .ForMember(c => c.FullName, cfg => cfg.MapFrom(cfg => cfg.FullName))
                .ForMember(c => c.Tag, cfg => cfg.MapFrom(cfg => cfg.Tag))
                .ForMember(c => c.Description, cfg => cfg.MapFrom(cfg => cfg.Description))
                .ForMember(c => c.LogoURL, cfg => cfg.MapFrom(cfg => cfg.LogoURL));
            this.CreateMap<OrganizerServiceModel, OrganizerDetailsServiceModel>();
            this.CreateMap<Organizer, OrganizerDetailsServiceModel>();
        }
    }
}
