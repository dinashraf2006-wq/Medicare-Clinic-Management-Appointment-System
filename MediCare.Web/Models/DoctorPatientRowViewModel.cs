namespace MediCare.Web.Models;

public class DoctorPatientRowViewModel
{
    public int PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public DateTime? NextVisitDate { get; set; }
    public string Status { get; set; } = "Stable";
    public int RecordsCount { get; set; }
    public int PrescriptionsCount { get; set; }
    public int DocumentsCount { get; set; }
}