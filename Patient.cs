namespace MediCare.Data.Entities;

// SHARED (owned by Member 1 - User Profiles). Fields taken from the ERD only.
public class Patient
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
