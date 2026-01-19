using AutoMapper;
using AutoMapper.QueryableExtensions;
using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Services.Brandings.Models;
using Balkana.Services.Teams.Models;
using Microsoft.EntityFrameworkCore;

namespace Balkana.Services.Brandings
{
    public class BrandingService : IBrandingService
    {
        private readonly ApplicationDbContext data;
        private readonly AutoMapper.IConfigurationProvider mapper;

        public BrandingService(ApplicationDbContext data, IMapper mapper)
        {
            this.data = data;
            this.mapper = mapper.ConfigurationProvider;
        }

        public BrandingQueryServiceModel All(
            string searchTerm = null,
            int currentPage = 1,
            int brandingsPerPage = int.MaxValue
        )
        {
            var brandingsQuery = this.data.Brandings
                .Include(b => b.Founder)
                .Include(b => b.Manager)
                .Include(b => b.Teams)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                brandingsQuery = brandingsQuery.Where(b => 
                    (b.FullName + " " + b.Tag).ToLower().Contains(searchTerm.ToLower()));
            }

            var totalBrandings = brandingsQuery.Count();

            var brandings = brandingsQuery
                .Skip((currentPage - 1) * brandingsPerPage)
                .Take(brandingsPerPage)
                .Select(b => new BrandingServiceModel
                {
                    Id = b.Id,
                    FullName = b.FullName,
                    Tag = b.Tag,
                    LogoURL = b.LogoURL,
                    yearFounded = b.yearFounded,
                    FounderId = b.FounderId,
                    ManagerId = b.ManagerId,
                    FounderName = b.Founder != null ? b.Founder.UserName : null,
                    ManagerName = b.Manager != null ? b.Manager.UserName : null
                })
                .ToList();

            return new BrandingQueryServiceModel
            {
                TotalBrandings = totalBrandings,
                CurrentPage = currentPage,
                BrandingsPerPage = brandingsPerPage,
                Brandings = brandings
            };
        }

        public int Create(
            string fullName,
            string tag,
            string logoUrl,
            int yearFounded,
            string? founderId = null,
            string? managerId = null
        )
        {
            var branding = new Branding
            {
                FullName = fullName,
                Tag = tag,
                LogoURL = logoUrl,
                yearFounded = yearFounded,
                FounderId = founderId,
                ManagerId = managerId
            };

            this.data.Brandings.Add(branding);
            this.data.SaveChanges();

            return branding.Id;
        }

        public BrandingDetailsServiceModel Details(int id)
        {
            var branding = this.data.Brandings
                .Where(b => b.Id == id)
                .Include(b => b.Founder)
                .Include(b => b.Manager)
                .Include(b => b.Teams)
                    .ThenInclude(t => t.Game)
                .FirstOrDefault();

            if (branding == null) return null;

            var teams = branding.Teams.Select(t => new TeamServiceModel
            {
                Id = t.Id,
                FullName = t.FullName,
                Tag = t.Tag,
                LogoURL = t.LogoURL,
                GameId = t.GameId,
                yearFounded = t.yearFounded,
                GameName = t.Game?.FullName ?? ""
            }).ToList();

            return new BrandingDetailsServiceModel
            {
                Id = branding.Id,
                FullName = branding.FullName,
                Tag = branding.Tag,
                LogoURL = branding.LogoURL,
                yearFounded = branding.yearFounded,
                FounderId = branding.FounderId,
                ManagerId = branding.ManagerId,
                FounderName = branding.Founder != null ? branding.Founder.UserName : null,
                ManagerName = branding.Manager != null ? branding.Manager.UserName : null,
                Teams = teams
            };
        }

        public int AbsoluteNumberOfBrandings()
        {
            return this.data.Brandings.Count();
        }
    }
}

