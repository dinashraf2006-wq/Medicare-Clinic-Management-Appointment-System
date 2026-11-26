using MediCare.Data.Entities;
using MediCare.Data.Repositories;
using MediCare.Services.Common;

namespace MediCare.Services.MedicalRecords;

public interface IPrescriptionService
{
    // Doctor side
    Task<IReadOnlyList<Prescription>> GetDoctorPrescriptionsAsync(int doctorId, string? search);
    Task<ServiceResult<int>> CreateAsync(int doctorId, CreatePrescriptionDto dto, bool sendNow);
    Task<ServiceResult> SendAsync(int doctorId, int prescriptionId);
    Task<ServiceResult> DeleteDraftAsync(int doctorId, int prescriptionId);
    Task<Prescription?> GetForDoctorAsync(int doctorId, int prescriptionId);

    // Patient side
    Task<IReadOnlyList<(PrescriptionItem Item, Prescription Prescription, MedicationSupply Supply)>> GetPatientMedicationsAsync(int patientId);
    Task<(PrescriptionItem Item, MedicationSupply Supply)?> GetPatientMedicationAsync(int patientId, int itemId);
    Task<Prescription?> GetForPatientAsync(int patientId, int prescriptionId);

    /// <summary>"Draft", "Sent" or "Refill due" (shown in the doctor's list).</summary>
    string GetStatusLabel(Prescription prescription);
}

public class PrescriptionService : IPrescriptionService
{
    private readonly IUnitOfWork _uow;

    public PrescriptionService(IUnitOfWork uow) => _uow = uow;

    // ===================== Doctor side =====================

    public Task<IReadOnlyList<Prescription>> GetDoctorPrescriptionsAsync(int doctorId, string? search) =>
        _uow.Prescriptions.GetByDoctorAsync(doctorId, search);

    public async Task<ServiceResult<int>> CreateAsync(int doctorId, CreatePrescriptionDto dto, bool sendNow)
    {
        var items = dto.Items.Where(i => !string.IsNullOrWhiteSpace(i.MedicationName)).ToList();
        if (items.Count == 0)
            return ServiceResult<int>.Fail("Add at least one medication.");

        if (items.Any(i => string.IsNullOrWhiteSpace(i.Dose) || i.DurationDays is < 1 or > 365))
            return ServiceResult<int>.Fail("Each medication needs a dose and a duration between 1 and 365 days.");

        if (!await _uow.MedicalRecords.DoctorHasPatientAsync(doctorId, dto.PatientId))
            return ServiceResult<int>.Fail("You can only prescribe for your own patients.");

        if (dto.MedicalRecordId.HasValue)
        {
            var record = await _uow.MedicalRecords.GetByIdAsync(dto.MedicalRecordId.Value);
            if (record is null || record.PatientId != dto.PatientId || record.DoctorId != doctorId)
                return ServiceResult<int>.Fail("The selected visit note does not match this patient.");
        }

        var now = DateTime.UtcNow;
        var prescription = new Prescription
        {
            PatientId = dto.PatientId,
            DoctorId = doctorId,
            MedicalRecordId = dto.MedicalRecordId,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            Status = sendNow ? PrescriptionStatus.Sent : PrescriptionStatus.Draft,
            CreatedAt = now,
            SentAt = sendNow ? now : null,
            Items = items.Select(i => new PrescriptionItem
            {
                MedicationName = i.MedicationName.Trim(),
                Indication = Clean(i.Indication),
                Dose = i.Dose.Trim(),
                Frequency = i.Frequency,
                Timing = Clean(i.Timing),
                DurationDays = i.DurationDays,
                Instructions = Clean(i.Instructions)
            }).ToList()
        };

        await _uow.Prescriptions.AddAsync(prescription);
        await _uow.SaveChangesAsync();
        return ServiceResult<int>.Ok(prescription.Id);
    }

    public async Task<ServiceResult> SendAsync(int doctorId, int prescriptionId)
    {
        var prescription = await _uow.Prescriptions.GetByIdAsync(prescriptionId);
        if (prescription is null || prescription.DoctorId != doctorId)
            return ServiceResult.Fail("Prescription not found.");
        if (prescription.Status == PrescriptionStatus.Sent)
            return ServiceResult.Fail("This prescription was already sent.");

        prescription.Status = PrescriptionStatus.Sent;
        prescription.SentAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteDraftAsync(int doctorId, int prescriptionId)
    {
        var prescription = await _uow.Prescriptions.GetByIdAsync(prescriptionId);
        if (prescription is null || prescription.DoctorId != doctorId)
            return ServiceResult.Fail("Prescription not found.");
        if (prescription.Status != PrescriptionStatus.Draft)
            return ServiceResult.Fail("Only drafts can be deleted. Sent prescriptions are part of the medical record.");

        _uow.Prescriptions.Remove(prescription);
        await _uow.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<Prescription?> GetForDoctorAsync(int doctorId, int prescriptionId)
    {
        var prescription = await _uow.Prescriptions.GetWithDetailsAsync(prescriptionId);
        if (prescription is null)
            return null;

        // Own prescriptions (including drafts), or sent ones of a shared patient.
        if (prescription.DoctorId == doctorId)
            return prescription;

        return prescription.Status == PrescriptionStatus.Sent
               && await _uow.MedicalRecords.DoctorHasPatientAsync(doctorId, prescription.PatientId)
            ? prescription
            : null;
    }

    // ===================== Patient side =====================

    public async Task<IReadOnlyList<(PrescriptionItem Item, Prescription Prescription, MedicationSupply Supply)>>
        GetPatientMedicationsAsync(int patientId)
    {
        var now = DateTime.UtcNow;
        var prescriptions = await _uow.Prescriptions.GetByPatientAsync(patientId, sentOnly: true);

        return prescriptions
            .SelectMany(p => p.Items.Select(i => (Item: i, Prescription: p,
                Supply: MedicationSupply.For(i, p.SentAt ?? p.CreatedAt, now))))
            .OrderByDescending(x => x.Supply.IsActive)
            .ThenByDescending(x => x.Prescription.SentAt)
            .ToList();
    }

    public async Task<(PrescriptionItem Item, MedicationSupply Supply)?> GetPatientMedicationAsync(int patientId, int itemId)
    {
        var item = await _uow.Prescriptions.GetItemWithDetailsAsync(itemId);
        if (item is null
            || item.Prescription.PatientId != patientId
            || item.Prescription.Status != PrescriptionStatus.Sent)
            return null;

        var p = item.Prescription;
        return (item, MedicationSupply.For(item, p.SentAt ?? p.CreatedAt, DateTime.UtcNow));
    }

    public async Task<Prescription?> GetForPatientAsync(int patientId, int prescriptionId)
    {
        var prescription = await _uow.Prescriptions.GetWithDetailsAsync(prescriptionId);
        return prescription is not null
               && prescription.PatientId == patientId
               && prescription.Status == PrescriptionStatus.Sent
            ? prescription
            : null;
    }

    public string GetStatusLabel(Prescription prescription)
    {
        if (prescription.Status == PrescriptionStatus.Draft)
            return "Draft";

        var start = prescription.SentAt ?? prescription.CreatedAt;
        var refillDue = prescription.Items.Any(i => MedicationSupply.For(i, start, DateTime.UtcNow).IsRefillDue);
        return refillDue ? "Refill due" : "Sent";
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
