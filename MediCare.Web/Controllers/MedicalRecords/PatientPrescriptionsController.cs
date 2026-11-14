using MediCare.Services.MedicalRecords;
using MediCare.Web.Models.MedicalRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers.MedicalRecords;

[Authorize(Roles = "Patient")]
[Route("Patient/Prescriptions")]
public class PatientPrescriptionsController : MedicalRecordsControllerBase
{
    private readonly IPrescriptionService _prescriptions;

    public PatientPrescriptionsController(IPrescriptionService prescriptions, IProfileLookupService profiles)
        : base(profiles)
    {
        _prescriptions = prescriptions;
    }

    // GET /Patient/Prescriptions
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        var meds = await _prescriptions.GetPatientMedicationsAsync(patientId.Value);
        var cards = meds.Select(m => new MedicationCard { Item = m.Item, Prescription = m.Prescription, Supply = m.Supply }).ToList();

        return View("~/Views/PatientPrescriptions/Index.cshtml", cards);
    }

    // GET /Patient/Prescriptions/Medication/12
    [HttpGet("Medication/{itemId:int}")]
    public async Task<IActionResult> Medication(int itemId)
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        var found = await _prescriptions.GetPatientMedicationAsync(patientId.Value, itemId);
        if (found is null) return NotFound();

        var (item, supply) = found.Value;
        return View("~/Views/PatientPrescriptions/Medication.cshtml", new PatientMedicationDetailsViewModel
        {
            Item = item,
            Supply = supply,
            Instructions = MedicalRecordsDisplay.SplitLines(item.Instructions),
            Schedule = MedicalRecordsDisplay.DailySchedule(item.Frequency)
        });
    }

    // GET /Patient/Prescriptions/5/Print
    [HttpGet("{id:int}/Print")]
    public async Task<IActionResult> Print(int id)
    {
        var patientId = await CurrentPatientIdAsync();
        if (patientId is null) return MissingProfile();

        var prescription = await _prescriptions.GetForPatientAsync(patientId.Value, id);
        if (prescription is null) return NotFound();

        return View("~/Views/MedicalRecordsShared/PrescriptionPrint.cshtml", prescription);
    }
}
