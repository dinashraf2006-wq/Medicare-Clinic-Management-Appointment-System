using MediCare.Services.MedicalRecords;
using MediCare.Web.Models.MedicalRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers.MedicalRecords;

[Authorize(Roles = "Patient")]
[Route("Patient/MedicalRecords")]
public class PatientMedicalRecordsController : MedicalRecordsControllerBase
{
    private readonly IMedicalRecordService _records;

    public PatientMedicalRecordsController(IMedicalRecordService records, IProfileLookupService profiles)
        : base(profiles)
    {
        _records = records;
    }

    // GET /Patient/MedicalRecords?filter=imaging&q=x-ray
    [HttpGet("")]
    public async Task<IActionResult> Index(string? filter, string? q)
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        return View("~/Views/PatientMedicalRecords/Index.cshtml", await BuildIndexAsync(patientId.Value, filter, q, new UploadDocumentViewModel()));
    }

    // POST /Patient/MedicalRecords/Upload
    [HttpPost("Upload")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Upload([Bind(Prefix = "Upload")] UploadDocumentViewModel model)
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        // A patient always uploads to their own record, whatever the form says.
        model.PatientId = patientId;
        model.MedicalRecordId = null;
        ModelState.Remove("Upload.PatientId");

        if (ModelState.IsValid)
        {
            var result = await _records.UploadDocumentAsync(null, CurrentUserId, new UploadDocumentDto
            {
                PatientId = patientId.Value,
                Title = model.Title ?? string.Empty,
                DocumentType = model.DocumentType,
                File = ToUpload(model.File!)
            });

            if (result.Succeeded)
            {
                ShowSuccess("File uploaded", "It's now in your records.");
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("Upload.File", result.Error!);
        }

        ViewData["OpenUploadModal"] = true;
        return View("~/Views/PatientMedicalRecords/Index.cshtml", await BuildIndexAsync(patientId.Value, null, null, model));
    }

    // GET /Patient/MedicalRecords/Visits/5
    [HttpGet("Visits/{id:int}")]
    public async Task<IActionResult> Visit(int id)
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        var record = await _records.GetForPatientAsync(patientId.Value, id);
        if (record is null) return NotFound();

        return View("~/Views/PatientMedicalRecords/Visit.cshtml", record);
    }

    // GET /Patient/MedicalRecords/Documents/7/Download
    [HttpGet("Documents/{id:int}/Download")]
    public async Task<IActionResult> Download(int id, bool inline = false)
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        var opened = await _records.OpenDocumentForPatientAsync(patientId.Value, id);
        if (opened is null) return NotFound();

        return inline
            ? File(opened.Content, opened.Document.ContentType)
            : File(opened.Content, opened.Document.ContentType, opened.Document.OriginalFileName);
    }

    private async Task<PatientRecordsIndexViewModel> BuildIndexAsync(int patientId, string? filter, string? q, UploadDocumentViewModel upload) =>
        new()
        {
            Filter = string.IsNullOrWhiteSpace(filter) ? "all" : filter.ToLowerInvariant(),
            Search = q,
            Stats = await _records.GetPatientStatsAsync(patientId),
            Documents = await _records.GetPatientDocumentsAsync(patientId, filter, q),
            Visits = await _records.GetPatientVisitsAsync(patientId),
            Upload = upload
        };
}
