using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Data.Repositories;

public interface IMedicalDocumentRepository : IGenericRepository<MedicalDocument>
{
    Task<MedicalDocument?> GetWithPatientAsync(int id);
    Task<IReadOnlyList<MedicalDocument>> GetByPatientAsync(int patientId, IReadOnlyCollection<DocumentType>? types, string? search);

    /// <summary>Documents of every patient linked to the doctor.</summary>
    Task<IReadOnlyList<MedicalDocument>> GetForDoctorAsync(int doctorId, DocumentType? type, string? search, int take = 100);
}

public class MedicalDocumentRepository : GenericRepository<MedicalDocument>, IMedicalDocumentRepository
{
    public MedicalDocumentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<MedicalDocument?> GetWithPatientAsync(int id) =>
        Set.Include(d => d.Patient).ThenInclude(p => p.User)
           .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<IReadOnlyList<MedicalDocument>> GetByPatientAsync(
        int patientId, IReadOnlyCollection<DocumentType>? types, string? search)
    {
        var query = Set.Where(d => d.PatientId == patientId);

        if (types is { Count: > 0 })
            query = query.Where(d => types.Contains(d.DocumentType));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Title.Contains(term) || d.OriginalFileName.Contains(term));
        }

        return await query.Include(d => d.Doctor!).ThenInclude(doc => doc.User)
                          .OrderByDescending(d => d.UploadedAt)
                          .AsNoTracking()
                          .ToListAsync();
    }

    public async Task<IReadOnlyList<MedicalDocument>> GetForDoctorAsync(
        int doctorId, DocumentType? type, string? search, int take = 100)
    {
        var patientIds = Context.Appointments.Where(a => a.DoctorId == doctorId).Select(a => a.PatientId)
            .Union(Context.MedicalRecords.Where(r => r.DoctorId == doctorId).Select(r => r.PatientId))
            .Union(Context.Prescriptions.Where(p => p.DoctorId == doctorId).Select(p => p.PatientId))
            .Union(Set.Where(d => d.DoctorId == doctorId).Select(d => d.PatientId));

        var query = Set.Where(d => patientIds.Contains(d.PatientId));

        if (type.HasValue)
            query = query.Where(d => d.DocumentType == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Title.Contains(term) || d.Patient.User.FullName.Contains(term));
        }

        return await query.Include(d => d.Patient).ThenInclude(p => p.User)
                          .OrderByDescending(d => d.UploadedAt)
                          .Take(take)
                          .AsNoTracking()
                          .ToListAsync();
    }
}
