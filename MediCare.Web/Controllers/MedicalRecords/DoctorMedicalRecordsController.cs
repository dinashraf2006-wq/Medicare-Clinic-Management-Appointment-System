using MediCare.Services.MedicalRecords;
using MediCare.Web.Models.MedicalRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers.MedicalRecords;

[Authorize(Roles = "Doctor")]
[Route("Doctor/MedicalRecords")]
public class DoctorMedicalRecordsController : MedicalRecordsControllerBase
{
    private readonly IMedicalRecordService _records;

    public DoctorMedicalRecordsController(IMedicalRecordService records, IProfileLookupService profiles)
        : base(profiles)
    {
        _records = records;
    }

    // GET /Doctor/MedicalRecords?filter=ecg&q=sarah
    [HttpGet("")]
    public async Task<IActionResult> Index(string? filter, string? q)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        return View("~/Views/DoctorMedicalRecords/Index.cshtml", await BuildIndexAsync(doctorId.Value, filter, q, new UploadDocumentViewModel()));
    }

    // POST /Doctor/MedicalRecords/Upload
    [HttpPost("Upload")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Upload([Bind(Prefix = "Upload")] UploadDocumentViewModel model)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        if (ModelState.IsValid)
        {
            var result = await _records.UploadDocumentAsync(doctorId, CurrentUserId, new UploadDocumentDto
            {
                PatientId = model.PatientId!.Value,
                MedicalRecordId = model.MedicalRecordId,
                Title = model.Title ?? string.Empty,
                DocumentType = model.DocumentType,
                File = ToUpload(model.File!)
            });

            if (result.Succeeded)
            {
                ShowSuccess("File uploaded", "It's now in your records.");
                return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : (IActionResult)RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("Upload.File", result.Error!);
        }

        // Show the page again with the upload dialog open and the errors.
        ViewData["OpenUploadModal"] = true;
        return View("~/Views/DoctorMedicalRecords/Index.cshtml", await BuildIndexAsync(doctorId.Value, null, null, model));
    }

    // GET /Doctor/MedicalRecords/Create?patientId=1&appointmentId=3
    [HttpGet("Create")]
    public async Task<IActionResult> Create(int? patientId, int? appointmentId)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        var model = new ClinicalNoteViewModel { PatientId = patientId, AppointmentId = appointmentId };

        if (appointmentId.HasValue)
        {
            var appointment = await _records.GetAppointmentForNoteAsync(doctorId.Value, appointmentId.Value);
            if (appointment is null) return NotFound();
            model.PatientId = appointment.PatientId;
        }

        await FillNoteLookupsAsync(doctorId.Value, model);
        return View("~/Views/DoctorMedicalRecords/Create.cshtml", model);
    }

    // POST /Doctor/MedicalRecords/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> Create(ClinicalNoteViewModel model)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        if (model.Attachments.Count > 5)
            ModelState.AddModelError(nameof(model.Attachments), "You can attach up to 5 files at once.");

        if (ModelState.IsValid)
        {
            var dto = new CreateMedicalRecordDto
            {
                PatientId = model.PatientId!.Value,
                AppointmentId = model.AppointmentId,
                Symptoms = model.Symptoms,
                Diagnosis = model.Diagnosis,
                TreatmentNotes = model.TreatmentNotes,
                AttachmentType = model.AttachmentType,
                Attachments = model.Attachments.Where(f => f.Length > 0).Select(ToUpload).ToList()
            };

            var result = await _records.CreateAsync(doctorId.Value, CurrentUserId, dto);
            if (result.Succeeded)
            {
                ShowSuccess("Visit note saved", "The note was added to the patient's medical record.");
                return model.AddPrescription
                    ? (IActionResult)Redirect($"/Doctor/Prescriptions?patientId={model.PatientId}&recordId={result.Value}#new-prescription")
                    : RedirectToAction(nameof(Details), new { id = result.Value });
            }

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        await FillNoteLookupsAsync(doctorId.Value, model);
        return View("~/Views/DoctorMedicalRecords/Create.cshtml", model);
    }

    // GET /Doctor/MedicalRecords/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        var record = await _records.GetForDoctorAsync(doctorId.Value, id);
        if (record is null) return NotFound();

        ViewData["CurrentDoctorId"] = doctorId.Value;
        return View("~/Views/DoctorMedicalRecords/Details.cshtml", record);
    }

    // GET /Doctor/MedicalRecords/Patient/2  (full history of one patient)
    [HttpGet("Patient/{patientId:int}")]
    public async Task<IActionResult> PatientHistory(int patientId)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        var history = await _records.GetPatientHistoryForDoctorAsync(doctorId.Value, patientId);
        if (history is null) return NotFound();

        return View("~/Views/DoctorMedicalRecords/PatientHistory.cshtml", new PatientHistoryViewModel { History = history });
    }

    // GET /Doctor/MedicalRecords/Documents/7/Download
    [HttpGet("Documents/{id:int}/Download")]
    public async Task<IActionResult> Download(int id, bool inline = false)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        var opened = await _records.OpenDocumentForDoctorAsync(doctorId.Value, id);
        if (opened is null) return NotFound();

        return inline
            ? File(opened.Content, opened.Document.ContentType)
            : File(opened.Content, opened.Document.ContentType, opened.Document.OriginalFileName);
    }

    // ---------- helpers ----------

    private async Task<DoctorRecordsIndexViewModel> BuildIndexAsync(int doctorId, string? filter, string? q, UploadDocumentViewModel upload)
    {
        var feed = await _records.GetDoctorFeedAsync(doctorId, filter, q);
        var patients = await _records.GetDoctorPatientsAsync(doctorId);

        return new DoctorRecordsIndexViewModel
        {
            Filter = string.IsNullOrWhiteSpace(filter) ? "all" : filter.ToLowerInvariant(),
            Search = q,
            Items = feed,
            Patients = patients.Select(p => new PatientOption { Id = p.Id, Name = p.User.FullName }).ToList(),
            Upload = upload
        };
    }

    private async Task FillNoteLookupsAsync(int doctorId, ClinicalNoteViewModel model)
    {
        var patients = await _records.GetDoctorPatientsAsync(doctorId);
        model.Patients = patients.Select(p => new PatientOption { Id = p.Id, Name = p.User.FullName }).ToList();

        if (model.AppointmentId.HasValue)
        {
            var appointment = await _records.GetAppointmentForNoteAsync(doctorId, model.AppointmentId.Value);
            if (appointment is not null)
                model.AppointmentLabel = $"{appointment.AppointmentDate:ddd, MMM d} · {DateTime.Today.Add(appointment.StartTime):h:mm tt}";
        }
    }
}
