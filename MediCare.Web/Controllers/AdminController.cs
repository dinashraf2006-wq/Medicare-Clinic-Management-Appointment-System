using MediCare.Services.Admin;
using MediCare.Web.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminAnalyticsService _adminAnalyticsService;

    public AdminController(IAdminAnalyticsService adminAnalyticsService)
    {
        _adminAnalyticsService = adminAnalyticsService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var systemStats = await _adminAnalyticsService.GetSystemStatsAsync();
        var appointmentStats = await _adminAnalyticsService.GetAppointmentStatsAsync();
        var doctorStats = await _adminAnalyticsService.GetDoctorStatsAsync();
        var specializationStats = await _adminAnalyticsService.GetSpecializationStatsAsync();
        var revenueStats = await _adminAnalyticsService.GetRevenueStatsAsync();

        var viewModel = new AdminDashboardViewModel
        {
            SystemStats = systemStats,
            AppointmentStats = appointmentStats,
            DoctorStats = doctorStats.Take(10).ToList(),
            SpecializationStats = specializationStats.ToList(),
            RevenueStats = revenueStats
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Users()
    {
        var doctorStats = await _adminAnalyticsService.GetDoctorStatsAsync();
        var specializationStats = await _adminAnalyticsService.GetSpecializationStatsAsync();

        var viewModel = new AdminUsersViewModel
        {
            DoctorStats = doctorStats,
            SpecializationStats = specializationStats
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Analytics(DateTime? startDate, DateTime? endDate)
    {
        var appointmentStats = await _adminAnalyticsService.GetAppointmentStatsAsync(startDate, endDate);
        var revenueStats = await _adminAnalyticsService.GetRevenueStatsAsync(startDate, endDate);

        var viewModel = new AdminAnalyticsViewModel
        {
            AppointmentStats = appointmentStats,
            RevenueStats = revenueStats,
            StartDate = startDate,
            EndDate = endDate
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateDateRange(DateTime startDate, DateTime endDate)
    {
        return RedirectToAction(nameof(Analytics), new { startDate, endDate });
    }
}
