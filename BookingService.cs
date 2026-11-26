using MediCare.Data.Entities;
using MediCare.Data.Repositories;
using MediCare.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services.Booking;

public interface IBookingService
{
    Task<IReadOnlyList<BookingDoctorDto>> GetDoctorsAsync();
    Task<BookingDoctorDto?> GetDoctorAsync(int doctorId);

    /// <summary>All upcoming slots of a doctor in [from, to) with a free / taken flag.</summary>
    Task<IReadOnlyList<TimeSlotDto>> GetSlotsAsync(int doctorId, DateTime from, DateTime to);

    /// <summary>Runs every booking rule without saving (used by the checkout page).</summary>
    Task<ServiceResult> ValidateAsync(int patientId, int doctorId, DateTime start);

    Task<ServiceResult<int>> BookAsync(int patientId, BookAppointmentRequest request);

    Task<IReadOnlyList<PatientAppointmentDto>> GetPatientAppointmentsAsync(int patientId);
    Task<PatientAppointmentDto?> GetPatientAppointmentAsync(int patientId, int appointmentId);
    Task<ServiceResult> CancelAsync(int patientId, int appointmentId);
}

public class BookingService : IBookingService
{
    public const int MaxNotesLength = 500;
    public const string SlotTakenMessage = "Sorry, this slot was just booked by another patient. Please pick another time.";

    private readonly IUnitOfWork _uow;
    private readonly IDoctorAvailabilityProvider _availability;
    private readonly BookingOptions _options;
    private readonly TimeProvider _clock;

    public BookingService(IUnitOfWork uow, IDoctorAvailabilityProvider availability, BookingOptions options, TimeProvider clock)
    {
        _uow = uow;
        _availability = availability;
        _options = options;
        _clock = clock;
    }

    /// <summary>Clinic local time (appointments are stored as local date + time).</summary>
    private DateTime Now => _clock.GetLocalNow().DateTime;

    // ---------------- doctors ----------------

    public async Task<IReadOnlyList<BookingDoctorDto>> GetDoctorsAsync()
    {
        var doctors = await _uow.Repository<Doctor>().FindAsync(_ => true);
        var userIds = doctors.Select(d => d.UserId).ToList();
        var users = (await _uow.Repository<ApplicationUser>().FindAsync(u => userIds.Contains(u.Id)))
            .ToDictionary(u => u.Id, u => u.FullName);
        var specs = (await _uow.Repository<Specialization>().FindAsync(_ => true))
            .ToDictionary(s => s.Id, s => s.Name);

        return doctors
            .Select(d => ToDoctorDto(d, users.GetValueOrDefault(d.UserId), specs.GetValueOrDefault(d.SpecializationId)))
            .OrderBy(d => d.FullName)
            .ToList();
    }

    public async Task<BookingDoctorDto?> GetDoctorAsync(int doctorId)
    {
        var doctor = await _uow.Repository<Doctor>().GetByIdAsync(doctorId);
        if (doctor is null) return null;

        var user = await _uow.Repository<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == doctor.UserId);
        var spec = await _uow.Repository<Specialization>().GetByIdAsync(doctor.SpecializationId);
        return ToDoctorDto(doctor, user?.FullName, spec?.Name);
    }

    // ---------------- slots ----------------

    public async Task<IReadOnlyList<TimeSlotDto>> GetSlotsAsync(int doctorId, DateTime from, DateTime to)
    {
        var now = Now;
        var firstDay = from.Date < now.Date ? now.Date : from.Date;
        var lastDay = to.TimeOfDay == TimeSpan.Zero ? to.Date.AddDays(-1) : to.Date; // FullCalendar's end is exclusive
        var maxDay = now.Date.AddDays(_options.MaxDaysAhead);
        if (lastDay > maxDay) lastDay = maxDay;
        if (lastDay < firstDay) return Array.Empty<TimeSlotDto>();

        var booked = await _uow.Appointments.GetActiveForDoctorAsync(doctorId, firstDay, lastDay);
        var slots = new List<TimeSlotDto>();

        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            var periods = await _availability.GetWorkingPeriodsAsync(doctorId, DateOnly.FromDateTime(day));
            foreach (var period in periods)
            {
                for (var t = period.Start; t + _options.SlotLength <= period.End; t += _options.SlotLength)
                {
                    var start = day + t;
                    if (start < now + _options.MinLeadTime) continue;

                    var end = t + _options.SlotLength;
                    var taken = booked.Any(a => a.AppointmentDate.Date == day
                                             && a.StartTime < end
                                             && a.StartTime + _options.SlotLength > t);
                    slots.Add(new TimeSlotDto(start, start + _options.SlotLength, !taken));
                }
            }
        }

        return slots;
    }

    // ---------------- booking ----------------

    public async Task<ServiceResult> ValidateAsync(int patientId, int doctorId, DateTime start)
    {
        if (await _uow.Repository<Doctor>().GetByIdAsync(doctorId) is null)
            return ServiceResult.Fail("This doctor does not exist.");

        var now = Now;
        if (start < now + _options.MinLeadTime)
            return ServiceResult.Fail("This time has already passed. Please choose a later slot.");

        if (start.Date > now.Date.AddDays(_options.MaxDaysAhead))
            return ServiceResult.Fail($"You can book up to {_options.MaxDaysAhead} days ahead.");

        // The time must be one of the doctor's real slots (inside working hours and on the slot grid).
        var periods = await _availability.GetWorkingPeriodsAsync(doctorId, DateOnly.FromDateTime(start));
        var time = start.TimeOfDay;
        var onGrid = periods.Any(p => time >= p.Start
                                   && time + _options.SlotLength <= p.End
                                   && (time - p.Start).Ticks % _options.SlotLength.Ticks == 0);
        if (!onGrid)
            return ServiceResult.Fail("The doctor is not available at this time.");

        var end = time + _options.SlotLength;
        if (await _uow.Appointments.DoctorSlotTakenAsync(doctorId, start.Date, time, end, _options.SlotLength))
            return ServiceResult.Fail(SlotTakenMessage);

        if (await _uow.Appointments.PatientSlotTakenAsync(patientId, start.Date, time, end, _options.SlotLength))
            return ServiceResult.Fail("You already have another appointment at this time.");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<int>> BookAsync(int patientId, BookAppointmentRequest request)
    {
        var notes = string.IsNullOrWhiteSpace(request.PatientNotes) ? null : request.PatientNotes.Trim();
        if (notes is { Length: > MaxNotesLength })
            return ServiceResult<int>.Fail($"Notes can be at most {MaxNotesLength} characters.");

        var check = await ValidateAsync(patientId, request.DoctorId, request.Start);
        if (!check.Succeeded)
            return ServiceResult<int>.Fail(check.Error!);

        var appointment = new Appointment
        {
            DoctorId = request.DoctorId,
            PatientId = patientId,
            AppointmentDate = request.Start.Date,
            StartTime = request.Start.TimeOfDay,
            Status = AppointmentStatus.Pending,
            PatientNotes = notes
        };

        await _uow.Appointments.AddAsync(appointment);
        try
        {
            await _uow.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two patients confirmed the same slot at the same moment: the unique index
            // UX_Appointments_Doctor_Slot_Active only lets the first one in.
            _uow.Appointments.Remove(appointment);
            return ServiceResult<int>.Fail(SlotTakenMessage);
        }

        return ServiceResult<int>.Ok(appointment.Id);
    }

    // ---------------- patient's appointments ----------------

    public async Task<IReadOnlyList<PatientAppointmentDto>> GetPatientAppointmentsAsync(int patientId)
    {
        var items = await _uow.Appointments.GetByPatientAsync(patientId);
        return items.Select(ToDto).ToList();
    }

    public async Task<PatientAppointmentDto?> GetPatientAppointmentAsync(int patientId, int appointmentId)
    {
        var a = await _uow.Appointments.GetWithDetailsAsync(appointmentId);
        return a is null || a.PatientId != patientId ? null : ToDto(a);
    }

    public async Task<ServiceResult> CancelAsync(int patientId, int appointmentId)
    {
        var a = await _uow.Appointments.GetByIdAsync(appointmentId);
        if (a is null || a.PatientId != patientId)
            return ServiceResult.Fail("Appointment not found.");

        if (!CanCancel(a))
            return ServiceResult.Fail(AppointmentStatus.IsActive(a.Status)
                ? $"Appointments can only be cancelled at least {_options.CancelCutoff.TotalHours:0} hours before they start."
                : "This appointment can no longer be cancelled.");

        a.Status = AppointmentStatus.Cancelled;
        await _uow.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    // ---------------- helpers ----------------

    private bool CanCancel(Appointment a) =>
        AppointmentStatus.IsActive(a.Status) && a.AppointmentDate.Date + a.StartTime - Now >= _options.CancelCutoff;

    private static BookingDoctorDto ToDoctorDto(Doctor d, string? fullName, string? specialization) =>
        new(d.Id, fullName ?? "Doctor", specialization ?? "", d.ConsultationFee, d.Bio, d.ProfileImageUrl);

    private PatientAppointmentDto ToDto(Appointment a)
    {
        var start = a.AppointmentDate.Date + a.StartTime;
        var doctor = ToDoctorDto(a.Doctor, a.Doctor.User?.FullName, a.Doctor.Specialization?.Name);
        return new PatientAppointmentDto(a.Id, doctor, start, start + _options.SlotLength, a.Status, a.PatientNotes, CanCancel(a));
    }
}
