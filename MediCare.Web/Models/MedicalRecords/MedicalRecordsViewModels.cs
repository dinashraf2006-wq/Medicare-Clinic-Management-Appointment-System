using System.ComponentModel.DataAnnotations;
using MediCare.Data.Entities;
using MediCare.Services.MedicalRecords;

namespace MediCare.Web.Models.MedicalRecords;

// ===================== Shared =====================

public class PatientOption
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public class UploadDocumentViewModel
{
    [Required(ErrorMessage = "Choose a patient.")]
    [Display(Name = "Patient")]
    public int? PatientId { get; set; }

    public int? MedicalRecordId { get; set; }

    [StringLength(200)]
    public string? Title { get; set; }

    [Display(Name = "Type")]
    public DocumentType DocumentType { get; set; } = DocumentType.Report;

    [Required(ErrorMessage = "Choose a file to upload.")]
    public IFormFile? File { get; set; }

    public string? ReturnUrl { get; set; }
}

// ===================== Doctor =====================

public class DoctorRecordsIndexViewModel
{
    public string Filter { get; init; } = "all";
    public string? Search { get; init; }
    public IReadOnlyList<RecordFeedItem> Items { get; init; } = Array.Empty<RecordFeedItem>();
    public IReadOnlyList<PatientOption> Patients { get; init; } = Array.Empty<PatientOption>();
    public UploadDocumentViewModel Upload { get; init; } = new();
}

public class ClinicalNoteViewModel
{
    [Required(ErrorMessage = "Choose a patient.")]
    [Display(Name = "Patient")]
    public int? PatientId { get; set; }

    public int? AppointmentId { get; set; }

    [Required(ErrorMessage = "Describe the symptoms.")]
    [StringLength(2000)]
    public string Symptoms { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a diagnosis.")]
    [StringLength(1000)]
    public string Diagnosis { get; set; } = string.Empty;

    [StringLength(4000)]
    [Display(Name = "Treatment notes")]
    public string? TreatmentNotes { get; set; }

    [Display(Name = "Attachment type")]
    public DocumentType AttachmentType { get; set; } = DocumentType.Imaging;

    public List<IFormFile> Attachments { get; set; } = new();

    [Display(Name = "Write a prescription after saving")]
    public bool AddPrescription { get; set; }

    // Display only
    public IReadOnlyList<PatientOption> Patients { get; set; } = Array.Empty<PatientOption>();
    public string? AppointmentLabel { get; set; }
}

public class PrescriptionItemInput
{
    [Required(ErrorMessage = "Medication is required.")]
    [StringLength(150)]
    [Display(Name = "Medication")]
    public string MedicationName { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Used for")]
    public string? Indication { get; set; }

    [Required(ErrorMessage = "Dose is required.")]
    [StringLength(100)]
    public string Dose { get; set; } = "1 tablet";

    public MedicationFrequency Frequency { get; set; } = MedicationFrequency.OnceDaily;

    [StringLength(100)]
    public string? Timing { get; set; }

    [Range(1, 365, ErrorMessage = "Duration must be 1 to 365 days.")]
    [Display(Name = "Duration (days)")]
    public int DurationDays { get; set; } = 30;

    [StringLength(1000)]
    public string? Instructions { get; set; }
}

public class NewPrescriptionViewModel
{
    [Required(ErrorMessage = "Choose a patient.")]
    [Display(Name = "Patient")]
    public int? PatientId { get; set; }

    public int? MedicalRecordId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<PrescriptionItemInput> Items { get; set; } = new() { new PrescriptionItemInput() };
}

public class PrescriptionRow
{
    public Prescription Prescription { get; init; } = null!;
    public string StatusLabel { get; init; } = string.Empty;
}

public class DoctorPrescriptionsViewModel
{
    public string? Search { get; init; }
    public IReadOnlyList<PrescriptionRow> Rows { get; init; } = Array.Empty<PrescriptionRow>();
    public IReadOnlyList<PatientOption> Patients { get; init; } = Array.Empty<PatientOption>();
    public NewPrescriptionViewModel NewPrescription { get; init; } = new();
    public string? LinkedVisitLabel { get; init; }
}

public class PrescriptionDetailsViewModel
{
    public Prescription Prescription { get; init; } = null!;
    public string StatusLabel { get; init; } = string.Empty;
    public bool CanEdit { get; init; }
}

public class PatientHistoryViewModel
{
    public PatientHistory History { get; init; } = null!;
}

// ===================== Patient =====================

public class PatientRecordsIndexViewModel
{
    public string Filter { get; init; } = "all";
    public string? Search { get; init; }
    public PatientRecordStats Stats { get; init; } = null!;
    public IReadOnlyList<MedicalDocument> Documents { get; init; } = Array.Empty<MedicalDocument>();
    public IReadOnlyList<MedicalRecord> Visits { get; init; } = Array.Empty<MedicalRecord>();
    public UploadDocumentViewModel Upload { get; init; } = new();
}

public class MedicationCard
{
    public PrescriptionItem Item { get; init; } = null!;
    public Prescription Prescription { get; init; } = null!;
    public MedicationSupply Supply { get; init; } = null!;
}

public class PatientMedicationDetailsViewModel
{
    public PrescriptionItem Item { get; init; } = null!;
    public MedicationSupply Supply { get; init; } = null!;
    public IReadOnlyList<string> Instructions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<(string Slot, string Time, bool Active)> Schedule { get; init; } =
        Array.Empty<(string, string, bool)>();
}
