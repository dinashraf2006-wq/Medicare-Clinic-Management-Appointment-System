using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Data.Repositories;

public interface IMedicalRecordRepository : IGenericRepository<MedicalRecord>
{
    Task<MedicalRecord?> GetWithDetailsAsync(int id);
    Task<IReadOnlyList<MedicalRecord>> GetByPatientAsync(int patientId);
    Task<IReadOnlyList<MedicalRecord>> GetByDoctorAsync(int doctorId, string? search, int take = 100);

    /// <summary>True when the doctor has treated (or is booked with) this patient.</summary>
    Task<bool> DoctorHasPatientAsync(int doctorId, int patientId);

    /// <summary>All patients linked to the doctor through appointments, notes, prescriptions or uploads.</summary>
    Task<IReadOnlyList<Patient>> GetDoctorPatientsAsync(int doctorId);
}

public class MedicalRecordRepository : GenericRepository<MedicalRecord>, IMedicalRecordRepository
{
    public MedicalRecordRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<MedicalRecord?> GetWithDetailsAsync(int id) =>
        Set.Include(r => r.Patient).ThenInclude(p => p.User)
           .Include(r => r.Doctor).ThenInclude(d => d.User)
           .Include(r => r.Doctor).ThenInclude(d => d.Specialization)
           .Include(r => r.Appointment)
           .Include(r => r.Documents)
           .Include(r => r.Prescriptions).ThenInclude(p => p.Items)
           .AsSplitQuery()
           .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IReadOnlyList<MedicalRecord>> GetByPatientAsync(int patientId) =>
        await Set.Where(r => r.PatientId == patientId)
                 .Include(r => r.Doctor).ThenInclude(d => d.User)
                 .Include(r => r.Doctor).ThenInclude(d => d.Specialization)
                 .OrderByDescending(r => r.CreatedAt)
                 .AsNoTracking()
                 .ToListAsync();

    public async Task<IReadOnlyList<MedicalRecord>> GetByDoctorAsync(int doctorId, string? search, int take = 100)
    {
        var query = Set.Where(r => r.DoctorId == doctorId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.Diagnosis.Contains(term) || r.Patient.User.FullName.Contains(term));
        }

        return await query.Include(r => r.Patient).ThenInclude(p => p.User)
                          .OrderByDescending(r => r.CreatedAt)
                          .Take(take)
                          .AsNoTracking()
                          .ToListAsync();
    }

    public async Task<bool> DoctorHasPatientAsync(int doctorId, int patientId) =>
        await Context.Appointments.AnyAsync(a => a.DoctorId == doctorId && a.PatientId == patientId)
        || await Set.AnyAsync(r => r.DoctorId == doctorId && r.PatientId == patientId)
        || await Context.Prescriptions.AnyAsync(p => p.DoctorId == doctorId && p.PatientId == patientId)
        || await Context.MedicalDocuments.AnyAsync(d => d.DoctorId == doctorId && d.PatientId == patientId);

    public async Task<IReadOnlyList<Patient>> GetDoctorPatientsAsync(int doctorId)
    {
        var ids = Context.Appointments.Where(a => a.DoctorId == doctorId).Select(a => a.PatientId)
            .Union(Set.Where(r => r.DoctorId == doctorId).Select(r => r.PatientId))
            .Union(Context.Prescriptions.Where(p => p.DoctorId == doctorId).Select(p => p.PatientId))
            .Union(Context.MedicalDocuments.Where(d => d.DoctorId == doctorId).Select(d => d.PatientId));

        return await Context.Patients.Where(p => ids.Contains(p.Id))
                                     .Include(p => p.User)
                                     .OrderBy(p => p.User.FullName)
                                     .AsNoTracking()
                                     .ToListAsync();
    }
}
