namespace MediCare.Data.Entities;

/// <summary>A working interval for one weekday. Multiple intervals allow lunch breaks.</summary>
public class WorkingHour
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}
