namespace MediCare.Web.Models;

public class DoctorAppointmentRowViewModel
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMeta { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string VisitType { get; set; } = "In-clinic";
    public string Reason { get; set; } = "General consultation";
    public string Status { get; set; } = "Pending";
}

public class DoctorAppointmentsViewModel
{
    public int TodayCount { get; set; }
    public int TodayCompletedCount { get; set; }
    public int PendingCount { get; set; }
    public int VideoVisitsWeekCount { get; set; }
    public int NoShowsMonthCount { get; set; }

    public string ActiveTab { get; set; } = "Requests";
    public string? Search { get; set; }

    public List<DoctorAppointmentRowViewModel> Appointments { get; set; } = new();
    public List<(int Id, string Name)> DoctorPatients { get; set; } = new();
}