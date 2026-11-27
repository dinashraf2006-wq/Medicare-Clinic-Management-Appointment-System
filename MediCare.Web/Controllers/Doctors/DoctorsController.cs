using MediCare.Data.Repositories;
using MediCare.Services.Doctors;
using MediCare.Web.Models.Doctors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MediCare.Web.Controllers.Doctors
{
    public class DoctorsController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly IUnitOfWork _unitOfWork;
        public DoctorsController(IDoctorService doctorService, IUnitOfWork unitOfWork)
        {
            _doctorService = doctorService;
            _unitOfWork = unitOfWork;
        }


        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? specialty, decimal? minFee, decimal? maxFee, string? city = null)
        {
            var doctors = await _doctorService.SearchDoctorsAsync(searchTerm, specialty, minFee, maxFee, city);
            var cities = await _doctorService.GetAvailableCitiesAsync();
            var specializations = await _unitOfWork.Specialization.GetAllWithDoctorsAsync();

            var viewModel = new DoctorSearchViewModel
            {
                DoctorName = searchTerm,
                Specialty = specialty,
                City = city,
                MinFee = minFee,
                MaxFee = maxFee,
                AvailableCities = cities.ToList(),
                Specialties = specializations
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem
                    {
                        Value = s.Name,
                        Text = s.Name
                    })
                    .ToList(),
                Doctors = doctors.ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
                return NotFound();

            var viewModel = new DoctorDetailsViewModel
            {
                Id = doctor.Id,
                FullName = doctor.FullName.StartsWith("Dr.", StringComparison.OrdinalIgnoreCase)
                    ? doctor.FullName
                    : $"Dr. {doctor.FullName}",
                SpecializationName = doctor.SpecializationName,
                City = doctor.City,
                ConsultationFee = doctor.ConsultationFee,
                Bio = string.IsNullOrWhiteSpace(doctor.Bio)
                    ? $"{doctor.FullName} is an experienced {doctor.SpecializationName} specialist dedicated to clear, patient-centered care and evidence-based treatment."
                    : doctor.Bio,
                ProfileImageUrl = doctor.ProfileImageUrl
            };

            return View(viewModel);
        }
    }
}

