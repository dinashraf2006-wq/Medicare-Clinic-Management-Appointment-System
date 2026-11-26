using MediCare.Data;
using MediCare.Data.Entities;
using MediCare.Services.Booking;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services.DoctorSchedule;

public interface IDoctorScheduleService
{
    Task<IReadOnlyList<WorkingHour>> GetWorkingHoursAsync(int doctorId);
    Task SaveWorkingHoursAsync(int doctorId, IReadOnlyCollection<WorkingHourInput> hours);
    Task<IReadOnlyList<DoctorVacation>> GetVacationsAsync(int doctorId);
    Task AddVacationAsync(int doctorId, DateTime start, DateTime end, string? reason);
    Task<bool> DeleteVacationAsync(int doctorId, int vacationId);
    Task<IReadOnlyList<Appointment>> GetAppointmentsAsync(int doctorId, DateTime? from = null, DateTime? to = null);
    Task<bool> UpdateAppointmentStatusAsync(int doctorId, int appointmentId, string status);
    Task<DoctorDashboardData> GetDashboardAsync(int doctorId);
}

public sealed record WorkingHourInput(
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsActive);

public sealed record DoctorDashboardData(
    int Pending,
    int Confirmed,
    int Today,
    int Upcoming);

public class DoctorScheduleService : IDoctorScheduleService
{
    private readonly ApplicationDbContext _db;

    public DoctorScheduleService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WorkingHour>> GetWorkingHoursAsync(int doctorId) =>
        await _db.WorkingHours
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToListAsync();

    public async Task SaveWorkingHoursAsync(
        int doctorId,
        IReadOnlyCollection<WorkingHourInput> hours)
    {
        foreach (var h in hours.Where(x => x.IsActive))
        {
            if (h.StartTime < TimeSpan.Zero ||
                h.EndTime > TimeSpan.FromDays(1) ||
                h.StartTime >= h.EndTime)
            {
                throw new ArgumentException(
                    "Each active working period must have a valid start time before its end time.");
            }
        }

        var active = hours
            .Where(x => x.IsActive)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToList();

        for (var i = 1; i < active.Count; i++)
        {
            if (active[i].DayOfWeek == active[i - 1].DayOfWeek &&
                active[i].StartTime < active[i - 1].EndTime)
            {
                throw new ArgumentException(
                    "Working periods on the same day cannot overlap.");
            }
        }

        var activeAppointments = await _db.Appointments
            .Where(a => a.DoctorId == doctorId &&
                a.AppointmentDate >= DateTime.Today &&
                (a.Status == AppointmentStatus.Pending ||
                 a.Status == AppointmentStatus.Confirmed))
            .Select(a => new { a.AppointmentDate, a.StartTime })
            .ToListAsync();

        foreach (var appointment in activeAppointments)
        {
            var weekday = appointment.AppointmentDate.DayOfWeek;

            var fits = hours.Any(h =>
                h.IsActive &&
                h.DayOfWeek == weekday &&
                appointment.StartTime >= h.StartTime &&
                appointment.StartTime + TimeSpan.FromMinutes(30) <= h.EndTime);

            if (!fits)
            {
                throw new ArgumentException(
                    "Your new working hours would exclude an existing pending or confirmed appointment. Resolve that appointment first.");
            }
        }

        var old = await _db.WorkingHours
            .Where(x => x.DoctorId == doctorId)
            .ToListAsync();

        _db.WorkingHours.RemoveRange(old);

        foreach (var h in hours)
        {
            if (h.IsActive)
            {
                _db.WorkingHours.Add(new WorkingHour
                {
                    DoctorId = doctorId,
                    DayOfWeek = h.DayOfWeek,
                    StartTime = h.StartTime,
                    EndTime = h.EndTime,
                    IsActive = true
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<DoctorVacation>> GetVacationsAsync(int doctorId) =>
        await _db.DoctorVacations
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.EndDate >= DateTime.Today)
            .OrderBy(x => x.StartDate)
            .ToListAsync();

    public async Task AddVacationAsync(
        int doctorId,
        DateTime start,
        DateTime end,
        string? reason)
    {
        start = start.Date;
        end = end.Date;

        if (start < DateTime.Today || end < start)
        {
            throw new ArgumentException("Choose a valid future date range.");
        }

        var conflict = await _db.DoctorVacations.AnyAsync(x =>
            x.DoctorId == doctorId &&
            x.StartDate <= end &&
            x.EndDate >= start);

        if (conflict)
        {
            throw new ArgumentException(
                "This vacation overlaps an existing vacation.");
        }

        var booked = await _db.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.AppointmentDate >= start &&
            a.AppointmentDate < end.AddDays(1) &&
            (a.Status == AppointmentStatus.Pending ||
             a.Status == AppointmentStatus.Confirmed));

        if (booked)
        {
            throw new ArgumentException(
                "There are pending or confirmed appointments in this period. Resolve them before adding this vacation.");
        }

        _db.DoctorVacations.Add(new DoctorVacation
        {
            DoctorId = doctorId,
            StartDate = start,
            EndDate = end,
            Reason = string.IsNullOrWhiteSpace(reason)
                ? null
                : reason.Trim()
        });

        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteVacationAsync(int doctorId, int vacationId)
    {
        var item = await _db.DoctorVacations
            .FirstOrDefaultAsync(x =>
                x.Id == vacationId &&
                x.DoctorId == doctorId);

        if (item is null)
            return false;

        _db.DoctorVacations.Remove(item);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<IReadOnlyList<Appointment>> GetAppointmentsAsync(
        int doctorId,
        DateTime? from = null,
        DateTime? to = null) =>
        await _db.Appointments
            .Where(a =>
                a.DoctorId == doctorId &&
                (!from.HasValue || a.AppointmentDate >= from.Value.Date) &&
                (!to.HasValue || a.AppointmentDate <= to.Value.Date))
            .Include(a => a.Patient)
            .ThenInclude(p => p.User)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .AsNoTracking()
            .ToListAsync();

    // Updated: Allows cancelling confirmed appointments too.
    public async Task<bool> UpdateAppointmentStatusAsync(
        int doctorId,
        int appointmentId,
        string status)
    {
        if (status != AppointmentStatus.Confirmed &&
            status != AppointmentStatus.Cancelled)
        {
            return false;
        }

        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a =>
                a.Id == appointmentId &&
                a.DoctorId == doctorId);

        if (appointment is null)
            return false;

        if (appointment.Status != AppointmentStatus.Pending &&
            !(appointment.Status == AppointmentStatus.Confirmed &&
              status == AppointmentStatus.Cancelled))
        {
            return false;
        }

        appointment.Status = status;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<DoctorDashboardData> GetDashboardAsync(int doctorId)
    {
        var today = DateTime.Today;

        var all = _db.Appointments
            .Where(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate >= today);

        var pending = await all.CountAsync(a =>
            a.Status == AppointmentStatus.Pending);

        var confirmed = await all.CountAsync(a =>
            a.Status == AppointmentStatus.Confirmed);

        var todayCount = await all.CountAsync(a =>
            a.AppointmentDate == today &&
            (a.Status == AppointmentStatus.Pending ||
             a.Status == AppointmentStatus.Confirmed));

        var upcoming = await all.CountAsync(a =>
            a.Status == AppointmentStatus.Confirmed);

        return new DoctorDashboardData(
            pending,
            confirmed,
            todayCount,
            upcoming);
    }
}

/// <summary>
/// Database-backed availability used by patient booking.
/// </summary>
public class DatabaseDoctorAvailabilityProvider : IDoctorAvailabilityProvider
{
    private readonly ApplicationDbContext _db;

    public DatabaseDoctorAvailabilityProvider(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WorkingPeriod>> GetWorkingPeriodsAsync(
        int doctorId,
        DateOnly date)
    {
        var day = date.ToDateTime(TimeOnly.MinValue);

        if (await _db.DoctorVacations.AnyAsync(v =>
            v.DoctorId == doctorId &&
            v.StartDate <= day &&
            v.EndDate >= day))
        {
            return Array.Empty<WorkingPeriod>();
        }

        var periods = await _db.WorkingHours
            .AsNoTracking()
            .Where(x =>
                x.DoctorId == doctorId &&
                x.DayOfWeek == date.DayOfWeek &&
                x.IsActive)
            .OrderBy(x => x.StartTime)
            .Select(x => new WorkingPeriod(x.StartTime, x.EndTime))
            .ToListAsync();

        return periods;
    }
}