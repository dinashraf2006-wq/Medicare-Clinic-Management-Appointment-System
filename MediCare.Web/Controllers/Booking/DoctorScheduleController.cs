using System.Security.Claims;
using MediCare.Services.Booking;
using MediCare.Services.Common;
using MediCare.Services.MedicalRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers.Booking;

[Authorize(Roles = "Doctor")]
[Route("Doctor/Schedule")]
public class DoctorScheduleController(DoctorScheduleService schedule, IProfileLookupService profiles,
    BookingOptions options, TimeProvider clock) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(DateOnly? date, string? range)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return View("~/Views/MedicalRecordsShared/MissingProfile.cshtml");

        var now = options.ClinicNow(clock);
        var today = DateOnly.FromDateTime(now);
        var selected = date is { } value && value >= today.AddYears(-1) && value <= today.AddYears(1) ? value : today;
        var week = range == "week";
        var first = week ? selected.AddDays(-((int)selected.DayOfWeek + 1) % 7) : selected;
        var last = week ? first.AddDays(6) : first;
        return View(new DoctorSchedulePage(selected, today, week, first, last, now,
            await schedule.GetAgendaAsync(doctorId.Value, first, last),
            await schedule.GetWorkingHoursAsync(doctorId.Value),
            await schedule.GetVacationDaysAsync(doctorId.Value, today)));
    }

    [HttpPost("Hours")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Hours(List<ScheduleHoursInput> days) =>
        SaveAsync(doctorId => ModelState.IsValid
            ? schedule.SetWorkingHoursAsync(doctorId, days.Select(day => new ScheduleDay(
                day.Day, day.Start?.ToTimeSpan(), day.End?.ToTimeSpan())).ToList())
            : Task.FromResult(ServiceResult.Fail("Enter valid working hours.")));

    [HttpPost("Vacation")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Vacation(DateOnly date) =>
        SaveAsync(doctorId => schedule.AddVacationAsync(doctorId, date));

    [HttpPost("Vacation/Remove")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RemoveVacation(DateOnly date) => SaveAsync(async doctorId =>
    {
        await schedule.RemoveVacationAsync(doctorId, date);
        return ServiceResult.Ok();
    });

    [HttpPost("Appointments/{appointmentId:int}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Appointment(int appointmentId, string decision, DateOnly date, string? range) =>
        SaveAsync(doctorId => decision switch
        {
            "confirm" => schedule.UpdateAppointmentAsync(doctorId, appointmentId, confirm: true),
            "cancel" => schedule.UpdateAppointmentAsync(doctorId, appointmentId, confirm: false),
            _ => Task.FromResult(ServiceResult.Fail("Choose a valid appointment action."))
        }, date, range);

    private Task<int?> CurrentDoctorIdAsync() =>
        profiles.GetDoctorIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<IActionResult> SaveAsync(Func<int, Task<ServiceResult>> save, DateOnly? date = null, string? range = null)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return Forbid();
        var result = await save(doctorId.Value);
        if (result.Succeeded) TempData["ScheduleMessage"] = "Schedule updated.";
        else TempData["ErrorMessage"] = result.Error;
        return RedirectToAction(nameof(Index), new { date, range });
    }
}

public sealed class ScheduleHoursInput
{
    public DayOfWeek Day { get; set; }
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
}

public record DoctorSchedulePage(DateOnly SelectedDate, DateOnly Today, bool Week, DateOnly First, DateOnly Last, DateTime Now,
    IReadOnlyList<DoctorAgendaItem> Agenda, IReadOnlyList<ScheduleDay> Hours, IReadOnlyList<DateOnly> Vacation);
