using AutoMapper;
using Balkana.Data.Models;
using Balkana.Data.Services.Maps;

namespace Balkana.Data.Infrastructure
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            this.CreateMap<csMap, csMapServiceModel>();
        }
    }
}
