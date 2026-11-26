namespace MediCare.Data.Entities;

// SHARED (owned by Member 2 - Doctor Directory). Fields taken from the ERD only.
public class Specialization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
