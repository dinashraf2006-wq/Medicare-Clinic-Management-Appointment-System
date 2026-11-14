using MediCare.Data.Entities;
using MediCare.Services.Storage;

namespace MediCare.Services.MedicalRecords;

// ---------- Input ----------

public class CreateMedicalRecordDto
{
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string? TreatmentNotes { get; set; }

    public DocumentType AttachmentType { get; set; } = DocumentType.Report;
    public List<FileUpload> Attachments { get; set; } = new();
}

public class UploadDocumentDto
{
    public int PatientId { get; set; }
    public int? MedicalRecordId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public FileUpload File { get; set; } = new();
}

public class CreatePrescriptionDto
{
    public int PatientId { get; set; }
    public int? MedicalRecordId { get; set; }
    public string? Notes { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
}

public class PrescriptionItemDto
{
    public string MedicationName { get; set; } = string.Empty;
    public string? Indication { get; set; }
    public string Dose { get; set; } = string.Empty;
    public MedicationFrequency Frequency { get; set; } = MedicationFrequency.OnceDaily;
    public string? Timing { get; set; }
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }
}

// ---------- Output ----------

public enum RecordFeedKind { Document, ClinicalNote }

/// <summary>One row in the doctor's "Recent records" table (a file or a visit note).</summary>
public record RecordFeedItem(
    RecordFeedKind Kind,
    int Id,
    string Title,
    int PatientId,
    string PatientName,
    string TypeLabel,
    DateTime Date);

public record PatientRecordStats(
    int Reports, int ReportsThisMonth,
    int LabResults, int LabResultsThisMonth,
    int Visits, int? FirstVisitYear,
    int DoctorsWithAccess);

public record PatientHistory(
    Patient Patient,
    IReadOnlyList<MedicalRecord> Records,
    IReadOnlyList<Prescription> Prescriptions,
    IReadOnlyList<MedicalDocument> Documents);

public record OpenedDocument(MedicalDocument Document, Stream Content);

/// <summary>How much of a medication is left, based on when it was sent and its duration.</summary>
public record MedicationSupply(int DaysLeft, int PercentLeft, bool IsActive, bool IsRefillDue)
{
    public static MedicationSupply For(PrescriptionItem item, DateTime prescribedAtUtc, DateTime nowUtc)
    {
        if (item.DurationDays <= 0)
            return new MedicationSupply(0, 0, false, false);

        var end = prescribedAtUtc.AddDays(item.DurationDays);
        var daysLeft = (int)Math.Ceiling((end - nowUtc).TotalDays);
        daysLeft = Math.Clamp(daysLeft, 0, item.DurationDays);
        var percent = (int)Math.Round(daysLeft * 100.0 / item.DurationDays);
        var active = daysLeft > 0;

        return new MedicationSupply(daysLeft, percent, active, active && percent <= 20);
    }
}
