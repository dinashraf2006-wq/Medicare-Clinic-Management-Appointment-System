using System.Security.Claims;
using MediCare.Services.Booking;
using MediCare.Services.MedicalRecords;
using MediCare.Web.Models.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers.Booking;

/// <summary>Member 3 - Appointment Booking: pick a doctor, pick a slot (FullCalendar), checkout.</summary>
[Authorize(Roles = "Patient")]
[Route("Patient/Book")]
public class PatientBookingController : Controller
{
    private readonly IBookingService _booking;
    private readonly IProfileLookupService _profiles;
    private readonly BookingOptions _options;
    private readonly bool _isDemo;

    public PatientBookingController(IBookingService booking, IProfileLookupService profiles, BookingOptions options, IWebHostEnvironment environment)
    {
        _booking = booking;
        _profiles = profiles;
        _options = options;
        _isDemo = environment.IsEnvironment("Demo");
    }

    private Task<int?> CurrentPatientIdAsync() =>
        _profiles.GetPatientIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IActionResult MissingProfile() => View("~/Views/MedicalRecordsShared/MissingProfile.cshtml");

    // GET /Patient/Book  - choose a doctor (Member 2's directory links straight to /Patient/Book/{doctorId})
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        var doctors = await _booking.GetDoctorsAsync();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            doctors = doctors.Where(d => d.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                      || d.Specialization.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return View("~/Views/PatientBooking/Index.cshtml", new BookingDoctorsViewModel { Doctors = doctors, Search = q });
    }

    // GET /Patient/Book/5  - calendar with the doctor's free slots
    [HttpGet("{doctorId:int}")]
    public async Task<IActionResult> Doctor(int doctorId)
    {
        var doctor = await _booking.GetDoctorAsync(doctorId);
        if (doctor is null) return NotFound();

        return View("~/Views/PatientBooking/Doctor.cshtml", new BookingCalendarViewModel
        {
            Doctor = doctor,
            SlotMinutes = (int)_options.SlotLength.TotalMinutes,
            MaxDaysAhead = _options.MaxDaysAhead
        });
    }

    // GET /Patient/Book/5/Slots?start=...&end=...  - JSON feed for FullCalendar
    [HttpGet("{doctorId:int}/Slots")]
    public async Task<IActionResult> Slots(int doctorId, DateTime start, DateTime end)
    {
        if (!ModelState.IsValid || end <= start || (end - start).TotalDays > 42)
            return BadRequest();
        if (await _booking.GetDoctorAsync(doctorId) is null) return NotFound();

        var slots = await _booking.GetSlotsAsync(doctorId, start, end);
        var events = slots.Select(s => new CalendarSlotEvent(
            Id: BookingDisplay.Iso(s.Start),
            Title: s.IsAvailable ? "Available" : "Booked",
            Start: BookingDisplay.Iso(s.Start),
            End: BookingDisplay.Iso(s.End),
            Available: s.IsAvailable,
            ClassNames: new[] { s.IsAvailable ? "bk-slot-free" : "bk-slot-taken" }));

        return Json(events);
    }

    // GET /Patient/Book/5/Checkout?start=2026-11-22T10:00:00
    [HttpGet("{doctorId:int}/Checkout")]
    public async Task<IActionResult> Checkout(int doctorId, DateTime start)
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        var doctor = await _booking.GetDoctorAsync(doctorId);
        if (doctor is null) return NotFound();

        if (!ModelState.IsValid || start == default) return BadRequest();
        var check = await _booking.ValidateAsync(patientId.Value, doctorId, start);
        if (!check.Succeeded)
        {
            TempData["ErrorMessage"] = check.Error;
            return RedirectToAction(nameof(Doctor), new { doctorId });
        }

        return View("~/Views/PatientBooking/Checkout.cshtml", new CheckoutViewModel
        {
            Doctor = doctor,
            Start = start,
            End = start + _options.SlotLength
        });
    }

    // POST /Patient/Book/5/Checkout
    [HttpPost("{doctorId:int}/Checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int doctorId, CheckoutViewModel model)
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        var doctor = await _booking.GetDoctorAsync(doctorId);
        if (doctor is null) return NotFound();

        if (model.Start == default || model.Start > DateTime.MaxValue - _options.SlotLength)
            return BadRequest();
        model.Doctor = doctor;
        model.End = model.Start + _options.SlotLength;
        ModelState.Remove(nameof(CheckoutViewModel.Doctor));
        if (_isDemo) ModelState.Remove(nameof(CheckoutViewModel.PatientNotes));
        if (!ModelState.IsValid)
            return View("~/Views/PatientBooking/Checkout.cshtml", model);

        var result = await _booking.BookAsync(patientId.Value, new BookAppointmentRequest
        {
            DoctorId = doctorId,
            Start = model.Start,
            PatientNotes = _isDemo ? null : model.PatientNotes
        });

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction(nameof(Doctor), new { doctorId });
        }

        return Redirect($"/Patient/Appointments/{result.Value}/Confirmation");
    }
}
