using AutoMapper;
using Balkana.Data;
using Balkana.Data.Infrastructure.Extensions;
using Balkana.Models.Organizers;
using Balkana.Services.Organizers;
using Balkana.Services.Organizers.Models;
using Microsoft.AspNetCore.Mvc;

namespace Balkana.Controllers
{
    public class OrganizersController : Controller   
    {
        private readonly ApplicationDbContext data;
        private readonly IOrganizerService organizers;
        private readonly IMapper mapper;

        public OrganizersController(ApplicationDbContext data, IOrganizerService organizers, IMapper mapper)
        {
            this.data = data;
            this.organizers = organizers;
            this.mapper = mapper;
        }

        public IActionResult Index([FromQuery] AllOrganizersQueryModel query) 
        {
            var queryResult = this.organizers.All(
                query.SearchTerm,
                query.CurrentPage,
                AllOrganizersQueryModel.OrganizersPerPage);
            
            query.TotalOrgs = queryResult.TotalOrgs;
            query.Organizers = queryResult.Organizations;

            return View(query);
        }

        public IActionResult Add()
        {
            return View(new OrganizerFormModel());
        }

        [HttpPost]
        public IActionResult Add(OrganizerFormModel org)
        {
            if(!ModelState.IsValid)
            {
                return View(org);
            }

            var orgId = this.organizers.Create(
                org.FullName,
                org.Tag,
                org.Description,
                org.LogoURL);

            return RedirectToAction(nameof(Details), new { id = orgId, information = org.GetInformation() });
        }

        public IActionResult Details(int id, string information)
        {
            var org = this.organizers.Details(id);

            if(information != org.GetInformation())
            {
                return BadRequest();
            }

            return View(org);
        }

        public IActionResult Edit(int id)
        {
            var org = this.organizers.Details(id);

            var orgForm = this.mapper.Map<OrganizerFormModel>(org);
            
            return View(orgForm);
        }

        [HttpPost]
        public IActionResult Edit(int id, OrganizerFormModel orgForm)
        {
            if(!ModelState.IsValid)
            {
                return View(orgForm);
            }

            var edited = this.organizers.Edit(
                id,
                orgForm.FullName,
                orgForm.Tag,
                orgForm.Description,
                orgForm.LogoURL);

            if(!edited)
            {
                return BadRequest();
            }

            return RedirectToAction(nameof(Details), new { id, information = orgForm.GetInformation() });
        }
    }
}
