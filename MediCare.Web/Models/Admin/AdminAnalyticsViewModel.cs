using MediCare.Services.Admin;

namespace MediCare.Web.Models.Admin;

public class AdminAnalyticsViewModel
{
    public AppointmentStatsDto AppointmentStats { get; set; } = null!;
    public RevenueStatsDto RevenueStats { get; set; } = null!;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
