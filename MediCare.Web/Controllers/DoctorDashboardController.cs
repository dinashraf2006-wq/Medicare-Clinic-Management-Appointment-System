using MediCare.Data.Entities;
using MediCare.Data.Repositories;
using MediCare.Services.Booking;
using MediCare.Services.Doctors;
using MediCare.Services.MedicalRecords;
using MediCare.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


[Authorize(Roles = "Doctor")]
public class DoctorDashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDoctorService _doctorService;
    private readonly IMedicalRecordService _medicalRecordService;
    private readonly IPrescriptionService _prescriptionService;
    private readonly IDoctorAvailabilityProvider _availabilityProvider;

    public DoctorDashboardController(
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        IDoctorService doctorService,
        IMedicalRecordService medicalRecordService,
        IPrescriptionService prescriptionService,
        IDoctorAvailabilityProvider availabilityProvider)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _doctorService = doctorService;
        _medicalRecordService = medicalRecordService;
        _prescriptionService = prescriptionService;
        _availabilityProvider = availabilityProvider;
    }

    public async Task<IActionResult> Index(int? patientId = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var doctor = await _unitOfWork.Repository<Doctor>()
            .FirstOrDefaultAsync(d => d.UserId == user.Id);

        if (doctor == null)
            return NotFound("Doctor profile not found for this account.");

        var doctorDto = await _doctorService.GetDoctorByIdAsync(doctor.Id);
        var patients = await _medicalRecordService.GetDoctorPatientsAsync(doctor.Id);
        var feed = await _medicalRecordService.GetDoctorFeedAsync(doctor.Id, filter: null, search: null);
        var prescriptions = await _prescriptionService.GetDoctorPrescriptionsAsync(doctor.Id, search: null);

        var todayDateOnly = DateOnly.FromDateTime(DateTime.Today);
        var workingPeriods = await _availabilityProvider.GetWorkingPeriodsAsync(doctor.Id, todayDateOnly);

        var selectedPatientId = patientId ?? patients.FirstOrDefault()?.Id;
        PatientHistory? selectedHistory = selectedPatientId.HasValue
            ? await _medicalRecordService.GetPatientHistoryForDoctorAsync(doctor.Id, selectedPatientId.Value)
            : null;

        ViewBag.DoctorName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;
        ViewBag.DoctorInfo = doctorDto;
        ViewBag.PatientsCount = patients.Count;
        ViewBag.FeedCount = feed.Count;
        ViewBag.PrescriptionsCount = prescriptions.Count;
        ViewBag.WorkingPeriods = workingPeriods;

        return View((
            Patients: patients,
            Feed: feed,
            SelectedHistory: selectedHistory
        ));
    }

    public async Task<IActionResult> Patients(string? search = null, string? status = null, int? selectedPatientId = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var doctor = await _unitOfWork.Repository<Doctor>()
            .FirstOrDefaultAsync(d => d.UserId == user.Id);

        if (doctor == null)
            return NotFound("Doctor profile not found for this account.");

        var doctorDto = await _doctorService.GetDoctorByIdAsync(doctor.Id);
        var patients = await _medicalRecordService.GetDoctorPatientsAsync(doctor.Id);

        // Load appointments for this doctor to compute Last Visit & Next Visit accurately
        var doctorAppointments = await _unitOfWork.Repository<Appointment>()
            .FindAsync(a => a.DoctorId == doctor.Id && a.Status != "Cancelled");

        var appointmentsByPatient = doctorAppointments
            .GroupBy(a => a.PatientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var today = DateTime.Today;
        var rows = new List<DoctorPatientRowViewModel>();

        foreach (var p in patients)
        {
            var history = await _medicalRecordService.GetPatientHistoryForDoctorAsync(doctor.Id, p.Id);
            var pApps = appointmentsByPatient.TryGetValue(p.Id, out var list)
                ? list
                : new List<Appointment>();

            var lastVisitDate = pApps
                .Where(a => a.AppointmentDate.Date <= today)
                .OrderByDescending(a => a.AppointmentDate.Date)
                .ThenByDescending(a => a.StartTime)
                .Select(a => (DateTime?)a.AppointmentDate.Date)
                .FirstOrDefault()
                ?? history.Records.OrderByDescending(r => r.CreatedAt).Select(r => (DateTime?)r.CreatedAt.Date).FirstOrDefault();

            var nextVisitDate = pApps
                .Where(a => a.AppointmentDate.Date >= today && a.Status != "Completed")
                .OrderBy(a => a.AppointmentDate.Date)
                .ThenBy(a => a.StartTime)
                .Select(a => (DateTime?)a.AppointmentDate.Date)
                .FirstOrDefault();

            var latestRecord = history.Records.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
            var condition = !string.IsNullOrWhiteSpace(latestRecord?.Diagnosis)
                ? latestRecord.Diagnosis
                : (!string.IsNullOrWhiteSpace(latestRecord?.Symptoms)
                    ? latestRecord.Symptoms
                    : (!string.IsNullOrWhiteSpace(p.MedicalHistory) ? p.MedicalHistory : "General Consultation"));

            // Derive status cleanly from recent activity / allergies / history
            var combinedText = $"{condition} {p.Allergies} {p.MedicalHistory}".ToLowerInvariant();
            string computedStatus;
            if (combinedText.Contains("critical") || combinedText.Contains("emergency") || combinedText.Contains("heart failure") || combinedText.Contains("acute"))
            {
                computedStatus = "Critical";
            }
            else if (nextVisitDate.HasValue && (nextVisitDate.Value - today).TotalDays <= 7 || !string.IsNullOrWhiteSpace(p.Allergies))
            {
                computedStatus = "Monitoring";
            }
            else
            {
                computedStatus = "Stable";
            }

            var fullName = string.IsNullOrWhiteSpace(p.User?.FullName) ? $"Patient #{p.Id}" : p.User.FullName;

            rows.Add(new DoctorPatientRowViewModel
            {
                PatientId = p.Id,
                PatientCode = $"#PT-{p.Id:D5}",
                FullName = fullName,
                Email = p.User?.Email,
                Condition = condition,
                Allergies = p.Allergies,
                MedicalHistory = p.MedicalHistory,
                LastVisitDate = lastVisitDate,
                NextVisitDate = nextVisitDate,
                Status = computedStatus,
                RecordsCount = history.Records.Count,
                PrescriptionsCount = history.Prescriptions.Count,
                DocumentsCount = history.Documents.Count
            });
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            rows = rows.Where(r =>
                r.FullName.ToLowerInvariant().Contains(term) ||
                r.Condition.ToLowerInvariant().Contains(term) ||
                r.PatientCode.ToLowerInvariant().Contains(term) ||
                (r.Allergies != null && r.Allergies.ToLowerInvariant().Contains(term))).ToList();
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            rows = rows.Where(r => r.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var activeDetail = selectedPatientId.HasValue
            ? await _medicalRecordService.GetPatientHistoryForDoctorAsync(doctor.Id, selectedPatientId.Value)
            : null;

        ViewBag.DoctorName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;
        ViewBag.DoctorInfo = doctorDto;
        ViewBag.SearchTerm = search;
        ViewBag.StatusFilter = status ?? "All";
        ViewBag.SelectedPatientHistory = activeDetail;

        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Appointments(string? tab = "Requests", string? search = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var doctor = await _unitOfWork.Repository<Doctor>()
            .FirstOrDefaultAsync(d => d.UserId == user.Id);

        if (doctor == null)
            return NotFound("Doctor profile not found for this account.");

        var doctorDto = await _doctorService.GetDoctorByIdAsync(doctor.Id);
        var patients = await _medicalRecordService.GetDoctorPatientsAsync(doctor.Id);

        // Load all appointments for this doctor; include Patient + User if your generic repo supports includes,
        // or load patients dictionary to avoid null Patient.User
        var allAppointments = (await _unitOfWork.Repository<Appointment>()
            .FindAsync(a => a.DoctorId == doctor.Id)).ToList();

        var patientLookup = patients.ToDictionary(p => p.Id, p => p);
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        string ResolveType(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return "In-clinic";
            var lower = notes.ToLowerInvariant();
            return (lower.Contains("video") || lower.Contains("online") || lower.Contains("tele"))
                ? "Video"
                : "In-clinic";
        }

        var mapped = allAppointments
            .Select(a =>
            {
                patientLookup.TryGetValue(a.PatientId, out var p);
                var pName = p?.User?.FullName ?? $"Patient #{a.PatientId}";
                var reason = string.IsNullOrWhiteSpace(a.PatientNotes) ? "General consultation" : a.PatientNotes.Trim();
                var visitType = ResolveType(a.PatientNotes);

                return new DoctorAppointmentRowViewModel
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = pName,
                    PatientMeta = $"#PT-{a.PatientId:D5}",
                    AppointmentDate = a.AppointmentDate.Date,
                    StartTime = a.StartTime,
                    VisitType = visitType,
                    Reason = reason,
                    Status = a.Status
                };
            })
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToList();

        var activeTab = string.IsNullOrWhiteSpace(tab) ? "Requests" : tab;

        var filtered = mapped.Where(a => activeTab switch
        {
            "Upcoming" => a.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) && a.AppointmentDate >= today,
            "Completed" => a.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase),
            "Cancelled" => a.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || a.Status.Equals("Declined", StringComparison.OrdinalIgnoreCase),
            _ => a.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)
        });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            filtered = filtered.Where(a =>
                a.PatientName.ToLowerInvariant().Contains(term) ||
                a.Reason.ToLowerInvariant().Contains(term) ||
                a.PatientMeta.ToLowerInvariant().Contains(term));
        }

        var vm = new DoctorAppointmentsViewModel
        {
            TodayCount = mapped.Count(a => a.AppointmentDate == today && !a.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)),
            TodayCompletedCount = mapped.Count(a => a.AppointmentDate == today && a.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)),
            PendingCount = mapped.Count(a => a.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)),
            VideoVisitsWeekCount = mapped.Count(a =>
                a.VisitType == "Video" &&
                a.AppointmentDate >= weekStart &&
                a.AppointmentDate < weekEnd &&
                !a.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)),
            NoShowsMonthCount = mapped.Count(a =>
                a.AppointmentDate >= monthStart &&
                a.AppointmentDate < today &&
                (a.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || a.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))),
            ActiveTab = activeTab,
            Search = search,
            Appointments = filtered.ToList(),
            DoctorPatients = patients
                .Select(p => (p.Id, p.User?.FullName ?? $"Patient #{p.Id}"))
                .ToList()
        };

        ViewBag.DoctorName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;
        ViewBag.DoctorInfo = doctorDto;

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, string status, string? tab = "Requests", string? search = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var doctor = await _unitOfWork.Repository<Doctor>()
            .FirstOrDefaultAsync(d => d.UserId == user.Id);
        if (doctor == null)
            return NotFound();

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment == null || appointment.DoctorId != doctor.Id)
            return NotFound();

        var allowed = new[] { "Confirmed", "Cancelled", "Completed" };
        if (allowed.Contains(status))
        {
            appointment.Status = status;
            await _unitOfWork.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Appointments), new { tab, search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAppointment(int patientId, DateTime appointmentDate, TimeSpan startTime, string visitType = "In-clinic", string? patientNotes = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var doctor = await _unitOfWork.Repository<Doctor>()
            .FirstOrDefaultAsync(d => d.UserId == user.Id);
        if (doctor == null)
            return NotFound();

        var combinedNotes = string.IsNullOrWhiteSpace(patientNotes)
            ? visitType
            : $"{visitType} — {patientNotes.Trim()}";

        var appt = new Appointment
        {
            DoctorId = doctor.Id,
            PatientId = patientId,
            AppointmentDate = appointmentDate.Date,
            StartTime = startTime,
            Status = "Confirmed",
            PatientNotes = combinedNotes
        };

        await _unitOfWork.Appointments.AddAsync(appt);
        await _unitOfWork.SaveChangesAsync();

        return RedirectToAction(nameof(Appointments), new { tab = "Upcoming" });
    }
}