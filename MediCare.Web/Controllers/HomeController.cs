using MediCare.Web.Models;
using MediCare.Web.Models.Home;
using MediCare.Services.Doctors;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MediCare.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDoctorService _doctorService;

        public HomeController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
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

            var viewModel = new HomeViewModel
            {
                AvailableCities = cities.ToList(),
                TopDoctors = doctorCards,

                FeaturedSpecialties = new List<SpecialtyCardItem>
                {
                    new() { Id = 1, Name = "Cardiology", DoctorCount = 128, ImageUrl = "/images/specialties/cardiology.png" },
                    new() { Id = 2, Name = "Dermatology", DoctorCount = 96, ImageUrl = "/images/specialties/dermatology.png" },
                    new() { Id = 3, Name = "Orthopedics", DoctorCount = 88, ImageUrl = "/images/specialties/orthopedics.png" },
                    new() { Id = 4, Name = "Pediatrics", DoctorCount = 140, ImageUrl = "/images/specialties/pediatrics.png" },
                    new() { Id = 5, Name = "Gynecology", DoctorCount = 72, ImageUrl = "/images/specialties/gynecology.png" },
                    new() { Id = 6, Name = "Neurology", DoctorCount = 54, ImageUrl = "/images/specialties/neurology.png" },
                    new() { Id = 7, Name = "Dentistry", DoctorCount = 110, ImageUrl = "/images/specialties/dentistry.png" },
                    new() { Id = 8, Name = "Ophthalmology", DoctorCount = 63, ImageUrl = "/images/specialties/ophthalmology.png" }
                }
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
