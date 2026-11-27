namespace MediCare.Data.Entities;

public sealed class DoctorWorkingHours
{
    public int DoctorId { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
}

public sealed class DoctorVacationDay
{
    public int DoctorId { get; set; }
    public DateOnly Date { get; set; }
}
