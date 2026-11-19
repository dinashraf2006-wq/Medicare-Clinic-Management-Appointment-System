namespace MediCare.Data.Entities;

// Member 3 - Appointment Booking.
// Appointment.Status is stored as a string (ERD), these are the allowed values.
public static class AppointmentStatus
{
    public const string Pending = "Pending";     // booked by the patient, waiting for the doctor (Member 4 approves)
    public const string Confirmed = "Confirmed"; // approved by the doctor
    public const string Cancelled = "Cancelled"; // cancelled by the patient or the doctor - frees the slot
    public const string Completed = "Completed"; // visit happened

    /// <summary>Statuses that hold a time slot (used for double-booking checks).</summary>
    public static readonly string[] Active = { Pending, Confirmed };

    public static bool IsActive(string status) => status == Pending || status == Confirmed;
}
