namespace MediCare.Services.Booking;

/// <summary>One block of working time in a doctor's day, e.g. 09:00 - 13:00.</summary>
public record WorkingPeriod(TimeSpan Start, TimeSpan End);

/// <summary>
/// Tells the booking system when a doctor works on a given day.
/// Member 4 (Doctor Schedule) owns working hours and vacation days: when that module is ready it
/// registers its own implementation of this interface and booking automatically follows the real schedule.
/// </summary>
public interface IDoctorAvailabilityProvider
{
    /// <summary>Working periods for that day. Empty list = day off / vacation.</summary>
    Task<IReadOnlyList<WorkingPeriod>> GetWorkingPeriodsAsync(int doctorId, DateOnly date);
}

public class BookingOptions
{
    /// <summary>Length of one appointment.</summary>
    public TimeZoneInfo ClinicTimeZone { get; set; } = TimeZoneInfo.Local;
    public bool ConfirmAutomatically { get; set; }

    public DateTime ClinicNow(TimeProvider clock) => TimeZoneInfo.ConvertTime(clock.GetUtcNow(), ClinicTimeZone).DateTime;

    public TimeSpan SlotLength { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>How far ahead patients may book.</summary>
    public int MaxDaysAhead { get; set; } = 60;

    /// <summary>A slot must start at least this long from now to be bookable.</summary>
    public TimeSpan MinLeadTime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Patients can cancel up to this long before the appointment.</summary>
    public TimeSpan CancelCutoff { get; set; } = TimeSpan.FromHours(2);

    // Default clinic hours used until Member 4's schedule module exists.
    public TimeSpan DefaultDayStart { get; set; } = new(9, 0, 0);
    public TimeSpan DefaultDayEnd { get; set; } = new(17, 0, 0);
    public TimeSpan? DefaultBreakStart { get; set; } = new(13, 0, 0);
    public TimeSpan? DefaultBreakEnd { get; set; } = new(14, 0, 0);
    public DayOfWeek[] DefaultDaysOff { get; set; } = { DayOfWeek.Friday };
}

/// <summary>Temporary schedule: same clinic hours for every doctor (Sat-Thu, 9-5 with a lunch break).</summary>
public class DefaultDoctorAvailabilityProvider : IDoctorAvailabilityProvider
{
    private readonly BookingOptions _options;

    public DefaultDoctorAvailabilityProvider(BookingOptions options) => _options = options;

    public Task<IReadOnlyList<WorkingPeriod>> GetWorkingPeriodsAsync(int doctorId, DateOnly date)
    {
        if (_options.DefaultDaysOff.Contains(date.DayOfWeek))
            return Task.FromResult<IReadOnlyList<WorkingPeriod>>(Array.Empty<WorkingPeriod>());

        IReadOnlyList<WorkingPeriod> periods = _options.DefaultBreakStart is { } bs && _options.DefaultBreakEnd is { } be
            ? new[] { new WorkingPeriod(_options.DefaultDayStart, bs), new WorkingPeriod(be, _options.DefaultDayEnd) }
            : new[] { new WorkingPeriod(_options.DefaultDayStart, _options.DefaultDayEnd) };

        return Task.FromResult(periods);
    }
}
