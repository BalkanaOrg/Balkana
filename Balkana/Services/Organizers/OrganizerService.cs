using AutoMapper;
using AutoMapper.QueryableExtensions;
using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Services.Organizers.Models;

namespace Balkana.Services.Organizers
{
    public class OrganizerService : IOrganizerService
    {
        private readonly ApplicationDbContext data;
        private readonly IMapper mapper;
        private readonly AutoMapper.IConfigurationProvider con;

        public OrganizerService(ApplicationDbContext data, IMapper mapper) 
        {
            this.data = data;
            this.mapper = mapper;
            this.con = mapper.ConfigurationProvider;

        }

        public OrganizerQueryServiceModel All(
            string searchTerm = null,
            int currentPage = 1,
            int organizersPerPage = int.MaxValue
        )
        {
            var orgQuery = this.data.Organizers.AsQueryable();

            if(!string.IsNullOrWhiteSpace(searchTerm) )
            {
                orgQuery = orgQuery.Where(
                    c => c.FullName.ToLower().Contains(searchTerm.ToLower()) ||
                    c.Tag.ToLower().Contains(searchTerm.ToLower()) ||
                    c.Description.ToLower().Contains(searchTerm.ToLower()));
            }

            var totalOrgs = orgQuery.Count();

            var orgs = GetOrgs(orgQuery.Skip((currentPage - 1) * organizersPerPage).Take(organizersPerPage));

            return new OrganizerQueryServiceModel
            {
                TotalOrgs = totalOrgs,
                CurrentPage = currentPage,
                Organizations = orgs,
                OrgsPerPage = organizersPerPage
            };
        }

        public int Create(string FullName, string Tag, string Description, string LogoURL)
        {
            var orgData = new Organizer
            {
                FullName = FullName,
                Tag = Tag,
                Description = Description,
                LogoURL = LogoURL
            };
            this.data.Organizers.Add(orgData);
            this.data.SaveChanges();

            return orgData.Id;
        }

        public bool Edit(int id, string FullName, string Tag, string Description, string LogoURL)
        {
            var orgData = this.data.Organizers.Find(id);

            if(orgData == null)
            {
                return false;
            }

            orgData.FullName = FullName;
            orgData.Tag = Tag;
            orgData.Description = Description;
            orgData.LogoURL = LogoURL;

            this.data.SaveChanges();

            return true;
        }

        public OrganizerDetailsServiceModel Details(int id)
            => this.data
            .Organizers
            .Where(c => c.Id == id)
            .ProjectTo<OrganizerDetailsServiceModel>(this.con)
            .FirstOrDefault();


        public IEnumerable<OrganizerTournamentsServiceModel> AllTournaments()
            =>this.data
                .Tournaments
                .ProjectTo<OrganizerTournamentsServiceModel>(this.con)
                .ToList();

        public IEnumerable<OrganizerTournamentsServiceModel> AllTournaments(int organizerId)
            =>this.data
                .Tournaments
                .Where(c=>c.OrganizerId == organizerId)
                .ProjectTo<OrganizerTournamentsServiceModel>(this.con)
                .ToList();

        private IEnumerable<OrganizerServiceModel> GetOrgs(IQueryable<Organizer> orgQuery)
            => orgQuery
                .ProjectTo<OrganizerServiceModel>(this.con)
                .ToList();

        public IEnumerable<string> GetAllTournaments()
            =>this.data
                .Tournaments
                .Select(c=>c.FullName)
                .ToList();
    }
}
