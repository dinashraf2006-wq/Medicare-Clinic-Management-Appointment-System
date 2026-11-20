using MediCare.Web.Models;
using MediCare.Web.Models.Home;
using MediCare.Services.Doctors;
using MediCare.Web.Models.Specialties;
using MediCare.Services.Specializations;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MediCare.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly ISpecializationService _specializationService;

        public HomeController(IDoctorService doctorService, ISpecializationService specializationService)
        {
            _doctorService = doctorService;
            _specializationService = specializationService;
        }
        public async Task<IActionResult> Index()
        {
            var cities = await _doctorService.GetAvailableCitiesAsync();
            var topDoctors = await _doctorService.GetTopDoctorsAsync(4);
            var doctorCards = topDoctors.Select(d => new DoctorCardItem
            {
                Id = d.Id,
                FullName = d.FullName,
                Specialty = d.SpecializationName,
                City = d.City,
                ConsultationFee = d.ConsultationFee,
                ProfileImageUrl = d.ProfileImageUrl
            }).ToList();

            var specializations = await _specializationService.GetAllSpecializationsAsync();
            var specialtyCards = specializations.Select(s => new SpecialtyCardItem
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                DoctorCount = s.Doctors.Count,
                //ImageUrl = null 
            }).ToList();

            var viewModel = new HomeViewModel
            {
                AvailableCities = cities.ToList(),
                TopDoctors = doctorCards,
                FeaturedSpecialties = specialtyCards
            };

            return View(viewModel);
        }

        public IActionResult ForDoctors()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
