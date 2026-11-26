namespace MediCare.Data.Entities;

/// <summary>A date or date range on which a doctor is unavailable.</summary>
public class DoctorVacation
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}
