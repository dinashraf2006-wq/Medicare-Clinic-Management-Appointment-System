using MediCare.Data.Repositories;
using Microsoft.AspNetCore.Identity;
using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services.Admin;

public interface IAdminAnalyticsService
{
    Task<SystemStatsDto> GetSystemStatsAsync();
    Task<AppointmentStatsDto> GetAppointmentStatsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<DoctorStatsDto>> GetDoctorStatsAsync();
    Task<IEnumerable<SpecializationStatsDto>> GetSpecializationStatsAsync();
    Task<RevenueStatsDto> GetRevenueStatsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

public class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminAnalyticsService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<SystemStatsDto> GetSystemStatsAsync()
    {
        var users = _userManager.Users.ToList();
        var doctors = await _unitOfWork.Doctors.GetAllWithDetailsAsync();
        var patients = await _unitOfWork.Repository<Patient>().FindAsync(x => true);
        var appointments = await _unitOfWork.Repository<Appointment>().FindAsync(x => true);
        var medicalRecords = await _unitOfWork.MedicalRecords.FindAsync(x => true);
        var prescriptions = await _unitOfWork.Prescriptions.FindAsync(x => true);

        return new SystemStatsDto
        {
            TotalUsers = users.Count,
            TotalDoctors = doctors.Count(),
            TotalPatients = patients.Count(),
            TotalAppointments = appointments.Count(),
            TotalMedicalRecords = medicalRecords.Count(),
            TotalPrescriptions = prescriptions.Count(),
            ActiveDoctors = doctors.Count(d => d.Appointments.Any(a => a.AppointmentDate >= DateTime.Today)),
            PendingAppointments = appointments.Count(a => a.Status == "Pending"),
            CompletedAppointments = appointments.Count(a => a.Status == "Completed"),
            CancelledAppointments = appointments.Count(a => a.Status == "Cancelled")
        };
    }

    public async Task<AppointmentStatsDto> GetAppointmentStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var appointments = await _unitOfWork.Repository<Appointment>().FindAsync(x => true);
        
        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = endDate ?? DateTime.Today;
        
        var filteredAppointments = appointments
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate <= end);

        var dailyStats = filteredAppointments
            .GroupBy(a => a.AppointmentDate.Date)
            .Select(g => new DailyAppointmentStat
            {
                Date = g.Key,
                Count = g.Count(),
                Completed = g.Count(a => a.Status == "Completed"),
                Pending = g.Count(a => a.Status == "Pending"),
                Cancelled = g.Count(a => a.Status == "Cancelled")
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new AppointmentStatsDto
        {
            StartDate = start,
            EndDate = end,
            TotalAppointments = filteredAppointments.Count(),
            DailyStats = dailyStats
        };
    }

    public async Task<IEnumerable<DoctorStatsDto>> GetDoctorStatsAsync()
    {
        var doctors = await _unitOfWork.Doctors.GetAllWithDetailsAsync();
        
        return doctors.Select(d => new DoctorStatsDto
        {
            DoctorId = d.Id,
            DoctorName = d.User?.FullName ?? "Dr. Unknown",
            Specialization = d.Specialization?.Name ?? "General",
            TotalAppointments = d.Appointments.Count,
            CompletedAppointments = d.Appointments.Count(a => a.Status == "Completed"),
            PendingAppointments = d.Appointments.Count(a => a.Status == "Pending"),
            CancelledAppointments = d.Appointments.Count(a => a.Status == "Cancelled"),
            ConsultationFee = d.ConsultationFee,
            City = d.City
        })
        .OrderByDescending(d => d.TotalAppointments)
        .ToList();
    }

    public async Task<IEnumerable<SpecializationStatsDto>> GetSpecializationStatsAsync()
    {
        var specializations = await _unitOfWork.Specialization.GetAllWithDoctorsAsync();
        
        return specializations.Select(s => new SpecializationStatsDto
        {
            SpecializationId = s.Id,
            Name = s.Name,
            DoctorCount = s.Doctors.Count,
            TotalAppointments = s.Doctors.SelectMany(d => d.Appointments).Count(),
            Description = s.Description
        })
        .OrderByDescending(s => s.TotalAppointments)
        .ToList();
    }

    public async Task<RevenueStatsDto> GetRevenueStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var appointments = await _unitOfWork.Repository<Appointment>().FindAsync(x => true);
        var doctors = await _unitOfWork.Doctors.GetAllWithDetailsAsync();
        
        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = endDate ?? DateTime.Today;
        
        var completedAppointments = appointments
            .Where(a => a.Status == "Completed" && a.AppointmentDate >= start && a.AppointmentDate <= end);

        var doctorDict = doctors.ToDictionary(d => d.Id, d => d.ConsultationFee);
        
        var totalRevenue = completedAppointments.Sum(a => 
            doctorDict.TryGetValue(a.DoctorId, out var fee) ? fee : 0);

        var dailyRevenue = completedAppointments
            .GroupBy(a => a.AppointmentDate.Date)
            .Select(g => new DailyRevenueStat
            {
                Date = g.Key,
                Amount = g.Sum(a => doctorDict.TryGetValue(a.DoctorId, out var fee) ? fee : 0),
                AppointmentCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new RevenueStatsDto
        {
            StartDate = start,
            EndDate = end,
            TotalRevenue = totalRevenue,
            DailyStats = dailyRevenue
        };
    }
}

public class SystemStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalAppointments { get; set; }
    public int TotalMedicalRecords { get; set; }
    public int TotalPrescriptions { get; set; }
    public int ActiveDoctors { get; set; }
    public int PendingAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
}

public class AppointmentStatsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalAppointments { get; set; }
    public List<DailyAppointmentStat> DailyStats { get; set; } = new();
}

public class DailyAppointmentStat
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Cancelled { get; set; }
}

public class DoctorStatsDto
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public decimal ConsultationFee { get; set; }
    public string City { get; set; } = string.Empty;
}

public class SpecializationStatsDto
{
    public int SpecializationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DoctorCount { get; set; }
    public int TotalAppointments { get; set; }
    public string? Description { get; set; }
}

public class RevenueStatsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<DailyRevenueStat> DailyStats { get; set; } = new();
}

public class DailyRevenueStat
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int AppointmentCount { get; set; }
}
