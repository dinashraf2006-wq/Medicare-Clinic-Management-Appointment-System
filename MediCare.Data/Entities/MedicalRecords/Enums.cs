using System.ComponentModel.DataAnnotations;

namespace MediCare.Data.Entities;

public enum DocumentType
{
    Report = 1,
    Imaging = 2,   // X-ray, CT, MRI
    Lab = 3,
    ECG = 4,
    Echo = 5,
    Notes = 6,
    Discharge = 7
}

public enum PrescriptionStatus
{
    Draft = 1,
    Sent = 2
}

public enum MedicationFrequency
{
    [Display(Name = "Once daily")] OnceDaily = 1,
    [Display(Name = "Twice daily")] TwiceDaily = 2,
    [Display(Name = "Three times daily")] ThreeTimesDaily = 3,
    [Display(Name = "Four times daily")] FourTimesDaily = 4,
    [Display(Name = "As needed")] AsNeeded = 5
}
