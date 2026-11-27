using MediCare.Data;
using MediCare.Data.Entities;
using MediCare.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services.Booking;

public record ScheduleDay(DayOfWeek Day, TimeSpan? Start, TimeSpan? End);
public record DoctorAgendaItem(int Id, string Patient, DateTime Start, string Status);

public sealed class DoctorScheduleService(ApplicationDbContext db, BookingOptions options, TimeProvider clock)
{
    public async Task<IReadOnlyList<ScheduleDay>> GetWorkingHoursAsync(int doctorId)
    {
        var custom = await db.Doctors.Where(d => d.Id == doctorId).Select(d => d.HasCustomSchedule).SingleAsync();
        if (!custom)
        {
            return Enum.GetValues<DayOfWeek>().Select(day => options.DefaultDaysOff.Contains(day)
                ? new ScheduleDay(day, null, null)
                : new ScheduleDay(day, options.DefaultDayStart, options.DefaultDayEnd)).ToList();
        }

        var configured = await db.DoctorWorkingHours.Where(hours => hours.DoctorId == doctorId)
            .ToDictionaryAsync(hours => hours.Day);
        return Enum.GetValues<DayOfWeek>().Select(day => configured.TryGetValue(day, out var hours)
            ? new ScheduleDay(day, hours.Start, hours.End)
            : new ScheduleDay(day, null, null)).ToList();
    }

    public Task<List<DateOnly>> GetVacationDaysAsync(int doctorId, DateOnly from) =>
        db.DoctorVacationDays.Where(day => day.DoctorId == doctorId && day.Date >= from)
            .OrderBy(day => day.Date).Select(day => day.Date).ToListAsync();

    public async Task<IReadOnlyList<DoctorAgendaItem>> GetAgendaAsync(int doctorId, DateOnly first, DateOnly last)
    {
        var from = first.ToDateTime(TimeOnly.MinValue);
        var to = last.ToDateTime(TimeOnly.MinValue);
        var items = await db.Appointments.AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= from && a.AppointmentDate <= to)
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
            .Select(a => new { a.Id, Patient = a.Patient.User.FullName, a.AppointmentDate, a.StartTime, a.Status })
            .ToListAsync();
        return items.Select(a => new DoctorAgendaItem(a.Id, a.Patient, a.AppointmentDate.Date + a.StartTime, a.Status)).ToList();
    }

    public async Task<ServiceResult> SetWorkingHoursAsync(int doctorId, IReadOnlyCollection<ScheduleDay> days)
    {
        if (days.Count != 7 || days.Select(day => day.Day).Distinct().Count() != 7 ||
            days.Any(day => !Enum.IsDefined(day.Day) || !IsValidDay(day)))
            return ServiceResult.Fail("Enter both times for each working day. Times must follow the calendar's slot interval.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!await LockDoctorAsync(db, doctorId)) return ServiceResult.Fail("Doctor profile not found.");

        var hoursByDay = days.ToDictionary(day => day.Day);
        var now = options.ClinicNow(clock);
        var upcoming = await ActiveAppointments(doctorId).Where(a => a.AppointmentDate >= now.Date)
            .Select(a => new { a.AppointmentDate, a.StartTime }).ToListAsync();
        if (upcoming.Any(a => a.AppointmentDate.Date + a.StartTime >= now &&
            !Fits(hoursByDay[a.AppointmentDate.DayOfWeek], a.StartTime)))
            return ServiceResult.Fail("The new hours conflict with a future appointment. Reschedule or cancel it first.");

        await db.DoctorWorkingHours.Where(hours => hours.DoctorId == doctorId).ExecuteDeleteAsync();
        db.DoctorWorkingHours.AddRange(days.Where(day => day.Start.HasValue).Select(day => new DoctorWorkingHours
        {
            DoctorId = doctorId, Day = day.Day, Start = day.Start!.Value, End = day.End!.Value
        }));
        await db.Doctors.Where(d => d.Id == doctorId)
            .ExecuteUpdateAsync(update => update.SetProperty(d => d.HasCustomSchedule, true));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AddVacationAsync(int doctorId, DateOnly date)
    {
        var today = DateOnly.FromDateTime(options.ClinicNow(clock));
        if (date < today || date > today.AddYears(1)) return ServiceResult.Fail("Choose a date within the next year.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!await LockDoctorAsync(db, doctorId)) return ServiceResult.Fail("Doctor profile not found.");

        if (await ActiveAppointments(doctorId).AnyAsync(a => a.AppointmentDate == date.ToDateTime(TimeOnly.MinValue)))
            return ServiceResult.Fail("This day has active appointments. Reschedule or cancel them before taking the day off.");

        if (!await db.DoctorVacationDays.AnyAsync(day => day.DoctorId == doctorId && day.Date == date))
        {
            db.DoctorVacationDays.Add(new DoctorVacationDay { DoctorId = doctorId, Date = date });
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        return ServiceResult.Ok();
    }

    public async Task RemoveVacationAsync(int doctorId, DateOnly date) =>
        await db.DoctorVacationDays.Where(day => day.DoctorId == doctorId && day.Date == date).ExecuteDeleteAsync();

    public async Task<ServiceResult> UpdateAppointmentAsync(int doctorId, int appointmentId, bool confirm)
    {
        var now = options.ClinicNow(clock);
        var appointment = await ActiveAppointments(doctorId).AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment is null || appointment.AppointmentDate.Date + appointment.StartTime <= now)
            return ServiceResult.Fail("This appointment can no longer be changed.");

        var status = confirm ? AppointmentStatus.Confirmed : AppointmentStatus.Cancelled;
        var currentStatus = confirm ? AppointmentStatus.Pending : appointment.Status;
        var updated = await db.Appointments.Where(a => a.Id == appointmentId && a.DoctorId == doctorId && a.Status == currentStatus)
            .ExecuteUpdateAsync(update => update.SetProperty(a => a.Status, status));
        return updated == 1 ? ServiceResult.Ok() : ServiceResult.Fail("The appointment changed. Refresh and try again.");
    }

    internal static async Task<bool> LockDoctorAsync(ApplicationDbContext db, int doctorId) =>
        // The transaction holds this row until it finishes so a booking and a schedule change cannot pass each other.
        await db.Doctors.Where(d => d.Id == doctorId)
            .ExecuteUpdateAsync(update => update.SetProperty(d => d.HasCustomSchedule, d => d.HasCustomSchedule)) == 1;

    private IQueryable<Appointment> ActiveAppointments(int doctorId) =>
        db.Appointments.Where(a => a.DoctorId == doctorId &&
            (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed));

    private bool IsValidDay(ScheduleDay day) =>
        day.Start is null && day.End is null ||
        day.Start is { } start && day.End is { } end && start >= TimeSpan.Zero &&
        end < TimeSpan.FromDays(1) && end - start >= options.SlotLength &&
        start.Ticks % options.SlotLength.Ticks == 0 && end.Ticks % options.SlotLength.Ticks == 0;

    private bool Fits(ScheduleDay day, TimeSpan start) =>
        day.Start is { } open && day.End is { } close && open <= start && start + options.SlotLength <= close;
}

public sealed class StoredDoctorAvailabilityProvider(ApplicationDbContext db, BookingOptions options) : IDoctorAvailabilityProvider
{
    public async Task<IReadOnlyList<WorkingPeriod>> GetWorkingPeriodsAsync(int doctorId, DateOnly date)
    {
        if (await db.DoctorVacationDays.AnyAsync(day => day.DoctorId == doctorId && day.Date == date)) return [];

        if (!await db.Doctors.AnyAsync(d => d.Id == doctorId && d.HasCustomSchedule))
            return await new DefaultDoctorAvailabilityProvider(options).GetWorkingPeriodsAsync(doctorId, date);

        var hours = await db.DoctorWorkingHours.FindAsync(doctorId, date.DayOfWeek);
        return hours is null ? [] : [new WorkingPeriod(hours.Start, hours.End)];
    }
}
