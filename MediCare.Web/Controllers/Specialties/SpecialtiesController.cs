using MediCare.Services.Specializations;
using MediCare.Web.Models.Specialties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Web.Controllers.Specialties
{
    public class SpecialtiesController : Controller
    {
        private readonly ISpecializationService _specializationService;

        // Maps normalized specialty names to optional images in wwwroot/images/specialties/
        // Any specialty not listed here cleanly renders the ECG placeholder box.
        //private static readonly Dictionary<string, string> SpecialtyImages = new(StringComparer.OrdinalIgnoreCase)
        //{
        //    ["cardiology"] = "/images/specialties/cardiology.jpg",
        //    ["dermatology"] = "/images/specialties/dermatology.jpg",
        //    ["orthopedics"] = "/images/specialties/orthopedics.jpg",
        //    ["pediatrics"] = "/images/specialties/pediatrics.jpg",
        //    ["gynecology"] = "/images/specialties/gynecology.jpg",
        //    ["neurology"] = "/images/specialties/neurology.jpg"
        //};

        public SpecialtiesController(ISpecializationService specializationService)
        {
            _specializationService = specializationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var specializations = await _specializationService.GetAllSpecializationsAsync(search);

            var items = specializations.Select(s => new SpecialtyCardItem
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                DoctorCount = s.Doctors.Count,
                //ImageUrl = SpecialtyImages.TryGetValue(s.Name.Trim(), out var imagePath) ? imagePath : null
            }).ToList();


            var model = new SpecialtiesViewModel
            {
                SearchQuery = search,
                Specialties = items
            };

            return View(model);
        }
    }
}
