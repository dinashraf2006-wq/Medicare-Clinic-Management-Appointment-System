using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Data.Repositories;

public interface IPrescriptionRepository : IGenericRepository<Prescription>
{
    Task<Prescription?> GetWithDetailsAsync(int id);
    Task<IReadOnlyList<Prescription>> GetByDoctorAsync(int doctorId, string? search, int take = 50);
    Task<IReadOnlyList<Prescription>> GetByPatientAsync(int patientId, bool sentOnly);
    Task<PrescriptionItem?> GetItemWithDetailsAsync(int itemId);
}

public class PrescriptionRepository : GenericRepository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Prescription?> GetWithDetailsAsync(int id) =>
        Set.Include(p => p.Items)
           .Include(p => p.Patient).ThenInclude(pt => pt.User)
           .Include(p => p.Doctor).ThenInclude(d => d.User)
           .Include(p => p.Doctor).ThenInclude(d => d.Specialization)
           .Include(p => p.MedicalRecord)
           .AsSplitQuery()
           .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Prescription>> GetByDoctorAsync(int doctorId, string? search, int take = 50)
    {
        var query = Set.Where(p => p.DoctorId == doctorId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Patient.User.FullName.Contains(term)
                                  || p.Items.Any(i => i.MedicationName.Contains(term)));
        }

        return await query.Include(p => p.Items)
                          .Include(p => p.Patient).ThenInclude(pt => pt.User)
                          .OrderByDescending(p => p.CreatedAt)
                          .Take(take)
                          .AsSplitQuery()
                          .AsNoTracking()
                          .ToListAsync();
    }

    public async Task<IReadOnlyList<Prescription>> GetByPatientAsync(int patientId, bool sentOnly)
    {
        var query = Set.Where(p => p.PatientId == patientId);
        if (sentOnly)
            query = query.Where(p => p.Status == PrescriptionStatus.Sent);

        return await query.Include(p => p.Items)
                          .Include(p => p.Doctor).ThenInclude(d => d.User)
                          .OrderByDescending(p => p.SentAt ?? p.CreatedAt)
                          .AsSplitQuery()
                          .AsNoTracking()
                          .ToListAsync();
    }

    public Task<PrescriptionItem?> GetItemWithDetailsAsync(int itemId) =>
        Context.PrescriptionItems
               .Include(i => i.Prescription).ThenInclude(p => p.Doctor).ThenInclude(d => d.User)
               .Include(i => i.Prescription).ThenInclude(p => p.Doctor).ThenInclude(d => d.Specialization)
               .AsNoTracking()
               .FirstOrDefaultAsync(i => i.Id == itemId);
}
