using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;


namespace MediCare.Data.Repositories
{
    public class SpecializationRepository : GenericRepository<Specialization>, ISpecializationRepository
    {
        public SpecializationRepository(ApplicationDbContext context) : base(context){}
        public async Task<IReadOnlyList<Specialization>> GetAllWithDoctorsAsync(string? search = null)
        {
            var query = Set
                .Include(s => s.Doctors)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    (s.Description != null && s.Description.ToLower().Contains(term)));
            }

            return await query
                .OrderBy(s => s.Id)
                .ToListAsync();
        }
    }

    public interface ISpecializationRepository : IGenericRepository<Specialization>
    {
        Task<IReadOnlyList<Specialization>> GetAllWithDoctorsAsync(string? search = null);
    }
}
