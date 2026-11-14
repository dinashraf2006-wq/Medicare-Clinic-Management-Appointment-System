namespace MediCare.Data.Entities;

/// <summary>
/// One medication line on a prescription, e.g. "Amlodipine 5mg - 1 tablet - once daily - 30 days".
/// </summary>
public class PrescriptionItem
{
    public int Id { get; set; }

    public int PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;

    public string MedicationName { get; set; } = string.Empty;   // "Amlodipine 5mg"
    public string? Indication { get; set; }                       // "Blood pressure"
    public string Dose { get; set; } = string.Empty;             // "1 tablet"
    public MedicationFrequency Frequency { get; set; }
    public string? Timing { get; set; }                           // "after breakfast"
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }                     // one instruction per line
}
