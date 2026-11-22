using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Data.Repositories
{
    public class DoctorRepository : GenericRepository<Doctor>, IDoctorRepository
    {
        private readonly ApplicationDbContext _context;

        public DoctorRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Doctor>> SearchDoctorsAsync(
    string? searchTerm,
    string? specialty,  
    decimal? minFee,
    decimal? maxFee,
    string? city = null)
        {
            var query = _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .AsQueryable();

            // 1. Top bar "Specialty or doctor" text search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(d =>
                    d.User.FullName.ToLower().Contains(term) ||
                    d.Specialization.Name.ToLower().Contains(term));
            }

            // 2. Sidebar exact Specialty filter
            if (!string.IsNullOrWhiteSpace(specialty))
            {
                var spec = specialty.Trim().ToLower();
                query = query.Where(d => d.Specialization.Name.ToLower() == spec);
            }

            // 3. Top bar City dropdown
            if (!string.IsNullOrWhiteSpace(city))
            {
                var cityTerm = city.Trim().ToLower();
                query = query.Where(d => d.City.ToLower() == cityTerm);
            }

            // 4. Sidebar Consultation Fee range
            if (minFee.HasValue)
                query = query.Where(d => d.ConsultationFee >= minFee.Value);

            if (maxFee.HasValue)
                query = query.Where(d => d.ConsultationFee <= maxFee.Value);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Doctor>> GetAllWithDetailsAsync()
        {
            return await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .ToListAsync();
        }

        public async Task<IEnumerable<Doctor>> GetFeaturedDoctorsAsync(int count = 4)
        {
            return await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .OrderByDescending(d => d.ConsultationFee) // Example criteria for "featured"
                .Take(count)
                .ToListAsync();
        }

        public async Task<Doctor?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .FirstOrDefaultAsync(d => d.Id == id);
        }
    }

    public interface IDoctorRepository : IGenericRepository<Doctor>
    {
        Task<IEnumerable<Doctor>> GetAllWithDetailsAsync();
        Task<IEnumerable<Doctor>> GetFeaturedDoctorsAsync(int count = 4);
        Task<Doctor?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Doctor>> SearchDoctorsAsync(
            string? searchTerm,
            string? specialization,
            decimal? minFee,
            decimal? maxFee,
            string? city = null);
    }
}

