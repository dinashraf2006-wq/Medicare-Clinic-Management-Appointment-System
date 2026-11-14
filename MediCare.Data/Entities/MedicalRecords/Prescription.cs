namespace MediCare.Data.Entities;

/// <summary>
/// A digital prescription. One prescription can hold several medications (items).
/// </summary>
public class Prescription
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    // Optional link to the visit note it was written in.
    public int? MedicalRecordId { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }

    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
