using System.Security.Claims;
using MediCare.Data.Entities;
using MediCare.Services.Booking;
using MediCare.Services.MedicalRecords;
using MediCare.Web.Models.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers.Booking;

/// <summary>Member 3 - Appointment Booking: the patient's own appointments (list, confirmation, cancel).</summary>
[Authorize(Roles = "Patient")]
[Route("Patient/Appointments")]
public class PatientAppointmentsController : Controller
{
    private readonly IBookingService _booking;
    private readonly IProfileLookupService _profiles;
    private readonly BookingOptions _options;
    private readonly TimeProvider _clock;

    public PatientAppointmentsController(IBookingService booking, IProfileLookupService profiles, BookingOptions options, TimeProvider clock)
    {
        _booking = booking;
        _profiles = profiles;
        _options = options;
        _clock = clock;
    }

    private Task<int?> CurrentPatientIdAsync() =>
        _profiles.GetPatientIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IActionResult MissingProfile() => View("~/Views/MedicalRecordsShared/MissingProfile.cshtml");

    // GET /Patient/Appointments?tab=upcoming|history
    [HttpGet("")]
    public async Task<IActionResult> Index(string tab = "upcoming")
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        var all = await _booking.GetPatientAppointmentsAsync(patientId.Value);
        var now = _options.ClinicNow(_clock);
        bool IsUpcoming(PatientAppointmentDto a) => a.End > now && AppointmentStatus.IsActive(a.Status);

        return View("~/Views/PatientAppointments/Index.cshtml", new PatientAppointmentsViewModel
        {
            Tab = tab == "history" ? "history" : "upcoming",
            Upcoming = all.Where(IsUpcoming).OrderBy(a => a.Start).ToList(),
            History = all.Where(a => !IsUpcoming(a)).ToList()
        });
    }

    // GET /Patient/Appointments/7/Confirmation
    [HttpGet("{id:int}/Confirmation")]
    public async Task<IActionResult> Confirmation(int id)
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        var appointment = await _booking.GetPatientAppointmentAsync(patientId.Value, id);
        if (appointment is null) return NotFound();

        return View("~/Views/PatientAppointments/Confirmation.cshtml", appointment);
    }

    // POST /Patient/Appointments/7/Cancel
    [HttpPost("{id:int}/Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        var result = await _booking.CancelAsync(patientId.Value, id);
        if (result.Succeeded)
        {
            TempData["SuccessTitle"] = "Appointment cancelled";
            TempData["SuccessMessage"] = "The time slot is now free for other patients.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }
}
