namespace MediCare.Services.Booking;

public record BookingDoctorDto(
    int Id,
    string FullName,
    string Specialization,
    decimal ConsultationFee,
    string? Bio,
    string? ProfileImageUrl);

public record TimeSlotDto(DateTime Start, DateTime End, bool IsAvailable);

public class BookAppointmentRequest
{
    public int DoctorId { get; set; }
    public DateTime Start { get; set; }
    public string? PatientNotes { get; set; }
}

public record PatientAppointmentDto(
    int Id,
    BookingDoctorDto Doctor,
    DateTime Start,
    DateTime End,
    string Status,
    string? PatientNotes,
    bool CanCancel);
