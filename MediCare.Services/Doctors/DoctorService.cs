using MediCare.Data.Repositories;

namespace MediCare.Services.Doctors
{
    public interface IDoctorService
    {
        Task<IEnumerable<DoctorDto>> SearchDoctorsAsync(string? doctorName, string? specialty, decimal? minFee, decimal? maxFee, string? city = null);
        Task<IEnumerable<string>> GetAvailableCitiesAsync();
        Task<IEnumerable<DoctorDto>> GetTopDoctorsAsync(int count = 4);
    }

    public class DoctorService : IDoctorService
    {
        private readonly IUnitOfWork _unitOfWork; // Or whatever UnitOfWork/DbContext pattern your team uses

        public DoctorService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<DoctorDto>> SearchDoctorsAsync(string? doctorName, string? specialty, decimal? minFee, decimal? maxFee, string? city = null)
        {
            var doctors = await _unitOfWork.Doctors.SearchDoctorsAsync(doctorName, specialty, minFee, maxFee, city);

            return doctors.Select(d => new DoctorDto
            {
                Id = d.Id,
                FullName = d.User?.FullName ?? "Dr. Unknown",
                SpecializationName = d.Specialization?.Name ?? "General",
                City = string.IsNullOrWhiteSpace(d.City) ? "Cairo" : d.City,
                ConsultationFee = d.ConsultationFee,
                Bio = d.Bio,
                ProfileImageUrl = string.IsNullOrWhiteSpace(d.ProfileImageUrl) ? "/images/default-doctor.png" : d.ProfileImageUrl
            });
        }

        public async Task<IEnumerable<string>> GetAvailableCitiesAsync()
        {
            var doctors = await _unitOfWork.Doctors.GetAllWithDetailsAsync();
            return doctors
                .Select(d => d.City)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();
        }

        public async Task<IEnumerable<DoctorDto>> GetTopDoctorsAsync(int count = 4)
        {
            var topDoctors = await _unitOfWork.Doctors.GetAllWithDetailsAsync();
            return topDoctors
               .OrderByDescending(d => d.Id)
               .Take(count)
               .Select(d => new DoctorDto
            {
                Id = d.Id,
                FullName = d.User?.FullName ?? "Dr. Unknown",
                SpecializationName = d.Specialization?.Name ?? "General",
                City = string.IsNullOrWhiteSpace(d.City) ? "Cairo" : d.City,
                ConsultationFee = d.ConsultationFee,
                Bio = d.Bio,
                ProfileImageUrl = string.IsNullOrWhiteSpace(d.ProfileImageUrl) ? "/images/default-doctor.png" : d.ProfileImageUrl
            }).ToList();
        }
    }
}
