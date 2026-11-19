using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Data.Repositories;

// Member 3 - Appointment Booking.
public interface IAppointmentRepository : IGenericRepository<Appointment>
{
    /// <summary>Active (Pending/Confirmed) appointments of a doctor between two dates (inclusive).</summary>
    Task<IReadOnlyList<Appointment>> GetActiveForDoctorAsync(int doctorId, DateTime fromDate, DateTime toDate);

    Task<bool> DoctorSlotTakenAsync(int doctorId, DateTime date, TimeSpan start, TimeSpan end, TimeSpan slotLength);
    Task<bool> PatientSlotTakenAsync(int patientId, DateTime date, TimeSpan start, TimeSpan end, TimeSpan slotLength);

    Task<IReadOnlyList<Appointment>> GetByPatientAsync(int patientId);
    Task<Appointment?> GetWithDetailsAsync(int id);
}

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Appointment>> GetActiveForDoctorAsync(int doctorId, DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;
        return await Set.Where(a => a.DoctorId == doctorId
                                 && a.AppointmentDate >= from && a.AppointmentDate <= to
                                 && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
                        .AsNoTracking()
                        .ToListAsync();
    }

    // Two appointments overlap when existing.Start < new.End and new.Start < existing.End.
    // Every appointment lasts one slot, so existing.End = existing.Start + slotLength.
    public Task<bool> DoctorSlotTakenAsync(int doctorId, DateTime date, TimeSpan start, TimeSpan end, TimeSpan slotLength)
    {
        var day = date.Date;
        var earliest = start - slotLength;
        return Set.AnyAsync(a => a.DoctorId == doctorId && a.AppointmentDate == day
                              && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
                              && a.StartTime < end && a.StartTime > earliest);
    }

    public Task<bool> PatientSlotTakenAsync(int patientId, DateTime date, TimeSpan start, TimeSpan end, TimeSpan slotLength)
    {
        var day = date.Date;
        var earliest = start - slotLength;
        return Set.AnyAsync(a => a.PatientId == patientId && a.AppointmentDate == day
                              && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
                              && a.StartTime < end && a.StartTime > earliest);
    }

    public async Task<IReadOnlyList<Appointment>> GetByPatientAsync(int patientId) =>
        await Set.Where(a => a.PatientId == patientId)
                 .Include(a => a.Doctor).ThenInclude(d => d.User)
                 .Include(a => a.Doctor).ThenInclude(d => d.Specialization)
                 .OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime)
                 .AsSplitQuery()
                 .AsNoTracking()
                 .ToListAsync();

    public Task<Appointment?> GetWithDetailsAsync(int id) =>
        Set.Include(a => a.Doctor).ThenInclude(d => d.User)
           .Include(a => a.Doctor).ThenInclude(d => d.Specialization)
           .Include(a => a.Patient).ThenInclude(p => p.User)
           .AsSplitQuery()
           .FirstOrDefaultAsync(a => a.Id == id);
}
