using System.ComponentModel.DataAnnotations;
using MediCare.Services.Booking;

namespace MediCare.Web.Models.Booking;

// Member 3 - Appointment Booking.

public class BookingDoctorsViewModel
{
    public IReadOnlyList<BookingDoctorDto> Doctors { get; init; } = Array.Empty<BookingDoctorDto>();
    public string? Search { get; init; }
}

public class BookingCalendarViewModel
{
    public BookingDoctorDto Doctor { get; init; } = null!;
    public int SlotMinutes { get; init; }
    public int MaxDaysAhead { get; init; }
}

public class CheckoutViewModel
{
    public BookingDoctorDto Doctor { get; set; } = null!;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    [StringLength(BookingService.MaxNotesLength, ErrorMessage = "Notes can be at most {1} characters.")]
    [Display(Name = "Reason for visit / notes for the doctor")]
    public string? PatientNotes { get; set; }
}

public class PatientAppointmentsViewModel
{
    public string Tab { get; init; } = "upcoming";
    public IReadOnlyList<PatientAppointmentDto> Upcoming { get; init; } = Array.Empty<PatientAppointmentDto>();
    public IReadOnlyList<PatientAppointmentDto> History { get; init; } = Array.Empty<PatientAppointmentDto>();
}

/// <summary>One event in the format FullCalendar.js expects.</summary>
public record CalendarSlotEvent(string Id, string Title, string Start, string End, bool Available, string[] ClassNames);

public static class BookingDisplay
{
    public const string IsoFormat = "yyyy-MM-ddTHH:mm:ss";

    public static string Iso(DateTime value) => value.ToString(IsoFormat, System.Globalization.CultureInfo.InvariantCulture);

    public static string Day(DateTime value) => value.ToString("dddd, MMM d, yyyy");

    public static string Time(DateTime value) => value.ToString("h:mm tt");

    public static string StatusBadge(string status) => status switch
    {
        "Confirmed" => "badge-status-ok",
        "Pending" => "badge-status-warn",
        "Cancelled" => "badge-status-cancel",
        _ => "badge-status-muted"
    };

    public static string StatusText(string status) => status switch
    {
        "Pending" => "Waiting for doctor",
        _ => status
    };
}
