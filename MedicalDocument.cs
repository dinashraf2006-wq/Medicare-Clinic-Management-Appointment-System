namespace MediCare.Data.Entities;

/// <summary>
/// An uploaded file (X-ray, ECG, lab report, discharge summary...).
/// The file itself is stored on disk outside wwwroot; only its metadata lives here.
/// </summary>
public class MedicalDocument
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    // Null when the patient uploaded the file themselves.
    public int? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    // Optional link to the visit note the file belongs to.
    public int? MedicalRecordId { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }

    public string Title { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public string UploadedByUserId { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
