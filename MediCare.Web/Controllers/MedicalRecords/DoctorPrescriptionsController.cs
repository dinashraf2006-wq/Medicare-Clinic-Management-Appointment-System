using MediCare.Data.Entities;
using MediCare.Services.MedicalRecords;
using MediCare.Web.Models.MedicalRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers.MedicalRecords;

[Authorize(Roles = "Doctor")]
[Route("Doctor/Prescriptions")]
public class DoctorPrescriptionsController : MedicalRecordsControllerBase
{
    private readonly IPrescriptionService _prescriptions;
    private readonly IMedicalRecordService _records;

    public DoctorPrescriptionsController(IPrescriptionService prescriptions, IMedicalRecordService records,
        IProfileLookupService profiles) : base(profiles)
    {
        _prescriptions = prescriptions;
        _records = records;
    }

    // GET /Doctor/Prescriptions?q=amlo&patientId=2&recordId=5
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, int? patientId, int? recordId)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        var form = new NewPrescriptionViewModel { PatientId = patientId, MedicalRecordId = recordId };
        return View("~/Views/DoctorPrescriptions/Index.cshtml", await BuildIndexAsync(doctorId.Value, q, form));
    }

    // POST /Doctor/Prescriptions   (submitAction = "draft" or "send")
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewPrescription")] NewPrescriptionViewModel model, string submitAction)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        if (ModelState.IsValid)
        {
            var sendNow = string.Equals(submitAction, "send", StringComparison.OrdinalIgnoreCase);
            var result = await _prescriptions.CreateAsync(doctorId.Value, new CreatePrescriptionDto
            {
                PatientId = model.PatientId!.Value,
                MedicalRecordId = model.MedicalRecordId,
                Notes = model.Notes,
                Items = model.Items.Select(i => new PrescriptionItemDto
                {
                    MedicationName = i.MedicationName,
                    Indication = i.Indication,
                    Dose = i.Dose,
                    Frequency = i.Frequency,
                    Timing = i.Timing,
                    DurationDays = i.DurationDays,
                    Instructions = i.Instructions
                }).ToList()
            }, sendNow);

            if (result.Succeeded)
            {
                ShowSuccess(sendNow ? "Prescription sent" : "Draft saved",
                    sendNow ? "The patient can now see it in their portal." : "You can send it later from the list.");
                return RedirectToAction(nameof(Details), new { id = result.Value });
            }

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        return View("~/Views/DoctorPrescriptions/Index.cshtml", await BuildIndexAsync(doctorId.Value, null, model));
    }

    // GET /Doctor/Prescriptions/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        var prescription = await _prescriptions.GetForDoctorAsync(doctorId.Value, id);
        if (prescription is null) return NotFound();

        return View("~/Views/DoctorPrescriptions/Details.cshtml", new PrescriptionDetailsViewModel
        {
            Prescription = prescription,
            StatusLabel = _prescriptions.GetStatusLabel(prescription),
            CanEdit = prescription.DoctorId == doctorId && prescription.Status == PrescriptionStatus.Draft
        });
    }

    // POST /Doctor/Prescriptions/5/Send
    [HttpPost("{id:int}/Send")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int id)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        var result = await _prescriptions.SendAsync(doctorId.Value, id);
        if (result.Succeeded) ShowSuccess("Prescription sent", "The patient can now see it in their portal.");
        else ShowError(result.Error!);

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Doctor/Prescriptions/5/Delete   (drafts only)
    [HttpPost("{id:int}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        var result = await _prescriptions.DeleteDraftAsync(doctorId.Value, id);
        if (!result.Succeeded)
        {
            ShowError(result.Error!);
            return RedirectToAction(nameof(Details), new { id });
        }

        ShowSuccess("Draft deleted", "The draft prescription was removed.");
        return RedirectToAction(nameof(Index));
    }

    // GET /Doctor/Prescriptions/5/Print
    [HttpGet("{id:int}/Print")]
    public async Task<IActionResult> Print(int id)
    {
        var doctorId = await CurrentDoctorIdAsync();
        if (doctorId is null) return MissingProfile();

        var prescription = await _prescriptions.GetForDoctorAsync(doctorId.Value, id);
        if (prescription is null) return NotFound();

        return View("~/Views/MedicalRecordsShared/PrescriptionPrint.cshtml", prescription);
    }

    // ---------- helpers ----------

    private async Task<DoctorPrescriptionsViewModel> BuildIndexAsync(int doctorId, string? q, NewPrescriptionViewModel form)
    {
        var list = await _prescriptions.GetDoctorPrescriptionsAsync(doctorId, q);
        var patients = await _records.GetDoctorPatientsAsync(doctorId);

        string? visitLabel = null;
        if (form.MedicalRecordId.HasValue)
        {
            var record = await _records.GetForDoctorAsync(doctorId, form.MedicalRecordId.Value);
            if (record is null || record.DoctorId != doctorId)
                form.MedicalRecordId = null; // ignore a visit that is not ours
            else
            {
                form.PatientId = record.PatientId;
                visitLabel = $"{record.Diagnosis} · {MedicalRecordsDisplay.FriendlyDate(record.CreatedAt)}";
            }
        }

        if (form.Items.Count == 0)
            form.Items.Add(new PrescriptionItemInput());

        return new DoctorPrescriptionsViewModel
        {
            Search = q,
            Rows = list.Select(p => new PrescriptionRow { Prescription = p, StatusLabel = _prescriptions.GetStatusLabel(p) }).ToList(),
            Patients = patients.Select(p => new PatientOption { Id = p.Id, Name = p.User.FullName }).ToList(),
            NewPrescription = form,
            LinkedVisitLabel = visitLabel
        };
    }
}
