using MediCare.Services.Admin;

namespace MediCare.Web.Models.Admin;

public class AdminDashboardViewModel
{
    public SystemStatsDto SystemStats { get; set; } = null!;
    public AppointmentStatsDto AppointmentStats { get; set; } = null!;
    public List<DoctorStatsDto> DoctorStats { get; set; } = new();
    public List<SpecializationStatsDto> SpecializationStats { get; set; } = new();
    public RevenueStatsDto RevenueStats { get; set; } = null!;
}
