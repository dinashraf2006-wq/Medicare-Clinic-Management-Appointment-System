using MediCare.Services.Doctors;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MediCare.Web.Models.Doctors
{
    public class DoctorSearchViewModel
    {
        // Form search inputs (keeps fields populated after submitting)
        public string? DoctorName { get; set; }
        public string? Specialty { get; set; }
        public string? City { get; set; }
        public decimal? MinFee { get; set; }
        public decimal? MaxFee { get; set; }

        // Dynamic dropdown data
        public List<string> AvailableCities { get; set; } = new();

        // Search results
        public List<DoctorDto> Doctors { get; set; } = new();
        public List<SelectListItem> Specialties { get; set; } = new();
    }
}
