namespace MediCare.Data.Entities;

/// <summary>
/// A clinical visit note written by a doctor (ERD: "Medical records").
/// </summary>
public class MedicalRecord
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    // Optional: a note can be written for a booked appointment or as a walk-in visit.
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string? TreatmentNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MedicalDocument> Documents { get; set; } = new List<MedicalDocument>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
