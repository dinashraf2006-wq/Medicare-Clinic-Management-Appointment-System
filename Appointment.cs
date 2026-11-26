namespace MediCare.Data.Entities;

// SHARED (owned by Member 3 - Appointment Booking). Fields taken from the ERD only.
// Medical Records only reads appointments (to link a visit note to an appointment).
public class Appointment
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Status { get; set; } = "Pending";
    public string? PatientNotes { get; set; }
}
