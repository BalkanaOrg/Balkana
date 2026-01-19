using AutoMapper;
using Balkana.Data;
using Balkana.Data.Infrastructure.Extensions;
using Balkana.Models.Brandings;
using Balkana.Services.Brandings;
using Balkana.Services.Brandings.Models;
using Balkana.Services.Images;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Balkana.Controllers
{
    public class BrandingsController : Controller
    {
        private readonly ApplicationDbContext data;
        private readonly IBrandingService brandings;
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment env;

        public BrandingsController(ApplicationDbContext data, IBrandingService brandings, IMapper mapper, IWebHostEnvironment env)
        {
            this.data = data;
            this.brandings = brandings;
            this.mapper = mapper;
            this.env = env;
        }

        public IActionResult Index([FromQuery] AllBrandsQueryModel query)
        {
            var queryResult = this.brandings.All(
                query.SearchTerm,
                query.CurrentPage,
                AllBrandsQueryModel.BrandingsPerPage);

            var absoluteNumberBrandings = this.brandings.AbsoluteNumberOfBrandings();

            query.TotalBrandings = queryResult.TotalBrandings;
            query.Brandings = queryResult.Brandings;
            query.AbsoluteNumberOfBrandings = absoluteNumberBrandings;

            // Calculate starting rank for the partial view
            ViewBag.StartRank = (query.CurrentPage - 1) * AllBrandsQueryModel.BrandingsPerPage + 1;

            return View(query);
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Add()
        {
            return View(new BrandingFormModel());
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Add(BrandingFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? logoPath = null;

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                try
                {
                    logoPath = await ImageOptimizer.SaveWebpAsync(
                        model.LogoFile,
                        env.WebRootPath,
                        Path.Combine("uploads", "BrandLogos"),
                        maxWidth: 512,
                        maxHeight: 512,
                        quality: 85);
                }
                catch
                {
                    ModelState.AddModelError("", "Error saving uploaded file.");
                    return View(model);
                }
            }

            var brandingId = this.brandings.Create(
                model.FullName,
                model.Tag,
                logoPath ?? string.Empty,
                model.YearFounded,
                model.FounderId,
                model.ManagerId);

            var branding = this.brandings.Details(brandingId);
            string brandingInformation = branding.GetInformation();

            return RedirectToAction(nameof(Details), new { id = brandingId, information = brandingInformation });
        }

        public IActionResult Details(int id, string information)
        {
            var branding = this.brandings.Details(id);

            if (branding == null)
            {
                return NotFound();
            }

            if (information != branding.GetInformation())
            {
                return BadRequest();
            }

            return View(branding);
        }
    }
}

