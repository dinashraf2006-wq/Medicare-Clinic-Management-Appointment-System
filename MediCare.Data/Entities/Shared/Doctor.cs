namespace MediCare.Data.Entities;

// SHARED (owned by Members 2/4). Fields taken from the ERD only.
public class Doctor
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int SpecializationId { get; set; }
    public Specialization Specialization { get; set; } = null!;

    public string? Bio { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? ProfileImageUrl { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
