using MediCare.Data.Entities;
using MediCare.Data.Repositories;
using MediCare.Services.Common;
using MediCare.Services.Storage;

namespace MediCare.Services.MedicalRecords;

public interface IMedicalRecordService
{
    // Doctor side
    Task<IReadOnlyList<Patient>> GetDoctorPatientsAsync(int doctorId);
    Task<IReadOnlyList<RecordFeedItem>> GetDoctorFeedAsync(int doctorId, string? filter, string? search);
    Task<ServiceResult<int>> CreateAsync(int doctorId, string doctorUserId, CreateMedicalRecordDto dto);
    Task<MedicalRecord?> GetForDoctorAsync(int doctorId, int recordId);
    Task<PatientHistory?> GetPatientHistoryForDoctorAsync(int doctorId, int patientId);
    Task<Appointment?> GetAppointmentForNoteAsync(int doctorId, int appointmentId);

    // Patient side
    Task<IReadOnlyList<MedicalRecord>> GetPatientVisitsAsync(int patientId);
    Task<MedicalRecord?> GetForPatientAsync(int patientId, int recordId);

    // Documents (both sides)
    Task<ServiceResult<int>> UploadDocumentAsync(int? doctorId, string uploadedByUserId, UploadDocumentDto dto);
    Task<IReadOnlyList<MedicalDocument>> GetPatientDocumentsAsync(int patientId, string? filter, string? search);
    Task<PatientRecordStats> GetPatientStatsAsync(int patientId);
    Task<OpenedDocument?> OpenDocumentForDoctorAsync(int doctorId, int documentId);
    Task<OpenedDocument?> OpenDocumentForPatientAsync(int patientId, int documentId);
}

public class MedicalRecordService : IMedicalRecordService
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;

    public MedicalRecordService(IUnitOfWork uow, IFileStorageService storage)
    {
        _uow = uow;
        _storage = storage;
    }

    // ===================== Doctor side =====================

    public Task<IReadOnlyList<Patient>> GetDoctorPatientsAsync(int doctorId) =>
        _uow.MedicalRecords.GetDoctorPatientsAsync(doctorId);

    public async Task<IReadOnlyList<RecordFeedItem>> GetDoctorFeedAsync(int doctorId, string? filter, string? search)
    {
        filter = (filter ?? "all").Trim().ToLowerInvariant();
        var feed = new List<RecordFeedItem>();

        DocumentType? docType = filter switch
        {
            "ecg" => DocumentType.ECG,
            "echo" => DocumentType.Echo,
            "lab" => DocumentType.Lab,
            "notes" => DocumentType.Notes,
            "imaging" => DocumentType.Imaging,
            "report" => DocumentType.Report,
            "discharge" => DocumentType.Discharge,
            _ => null
        };

        var docs = await _uow.MedicalDocuments.GetForDoctorAsync(doctorId, docType, search);
        feed.AddRange(docs.Select(d => new RecordFeedItem(
            RecordFeedKind.Document, d.Id, d.Title, d.PatientId, d.Patient.User.FullName,
            d.DocumentType.ToString(), d.UploadedAt)));

        // Visit notes appear under "All" and "Notes".
        if (filter is "all" or "notes")
        {
            var notes = await _uow.MedicalRecords.GetByDoctorAsync(doctorId, search);
            feed.AddRange(notes.Select(r => new RecordFeedItem(
                RecordFeedKind.ClinicalNote, r.Id, $"Consultation notes - {r.Diagnosis}", r.PatientId,
                r.Patient.User.FullName, "Notes", r.CreatedAt)));
        }

        return feed.OrderByDescending(f => f.Date).Take(100).ToList();
    }

    public async Task<ServiceResult<int>> CreateAsync(int doctorId, string doctorUserId, CreateMedicalRecordDto dto)
    {
        if (!await _uow.MedicalRecords.DoctorHasPatientAsync(doctorId, dto.PatientId))
            return ServiceResult<int>.Fail("You can only write notes for your own patients.");

        if (dto.AppointmentId.HasValue)
        {
            var appointment = await GetAppointmentForNoteAsync(doctorId, dto.AppointmentId.Value);
            if (appointment is null || appointment.PatientId != dto.PatientId)
                return ServiceResult<int>.Fail("The selected appointment does not belong to this patient.");
        }

        foreach (var file in dto.Attachments)
        {
            var check = _storage.Validate(file);
            if (!check.Succeeded)
                return ServiceResult<int>.Fail(check.Error!);
        }

        var record = new MedicalRecord
        {
            PatientId = dto.PatientId,
            DoctorId = doctorId,
            AppointmentId = dto.AppointmentId,
            Symptoms = dto.Symptoms.Trim(),
            Diagnosis = dto.Diagnosis.Trim(),
            TreatmentNotes = string.IsNullOrWhiteSpace(dto.TreatmentNotes) ? null : dto.TreatmentNotes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        // Save the files first; if anything fails, remove what was already written.
        var saved = new List<StoredFile>();
        try
        {
            foreach (var file in dto.Attachments)
            {
                var result = await _storage.SaveAsync(file);
                if (!result.Succeeded)
                {
                    saved.ForEach(f => _storage.Delete(f.StoredFileName));
                    return ServiceResult<int>.Fail(result.Error!);
                }

                saved.Add(result.Value!);
                record.Documents.Add(new MedicalDocument
                {
                    PatientId = dto.PatientId,
                    DoctorId = doctorId,
                    Title = Path.GetFileNameWithoutExtension(file.FileName),
                    DocumentType = dto.AttachmentType,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    StoredFileName = result.Value!.StoredFileName,
                    ContentType = result.Value.ContentType,
                    FileSize = result.Value.Size,
                    UploadedByUserId = doctorUserId,
                    UploadedAt = DateTime.UtcNow
                });
            }

            await _uow.MedicalRecords.AddAsync(record);
            await _uow.SaveChangesAsync();
            return ServiceResult<int>.Ok(record.Id);
        }
        catch
        {
            saved.ForEach(f => _storage.Delete(f.StoredFileName));
            throw;
        }
    }

    public async Task<MedicalRecord?> GetForDoctorAsync(int doctorId, int recordId)
    {
        var record = await _uow.MedicalRecords.GetWithDetailsAsync(recordId);
        if (record is null)
            return null;

        // Any doctor treating this patient can read the patient's history.
        return await _uow.MedicalRecords.DoctorHasPatientAsync(doctorId, record.PatientId) ? record : null;
    }

    public async Task<PatientHistory?> GetPatientHistoryForDoctorAsync(int doctorId, int patientId)
    {
        if (!await _uow.MedicalRecords.DoctorHasPatientAsync(doctorId, patientId))
            return null;

        var patients = await _uow.MedicalRecords.GetDoctorPatientsAsync(doctorId);
        var patient = patients.First(p => p.Id == patientId);

        var records = await _uow.MedicalRecords.GetByPatientAsync(patientId);
        var prescriptions = (await _uow.Prescriptions.GetByPatientAsync(patientId, sentOnly: false))
            // Other doctors' drafts are private to them.
            .Where(p => p.Status == PrescriptionStatus.Sent || p.DoctorId == doctorId)
            .ToList();
        var documents = await _uow.MedicalDocuments.GetByPatientAsync(patientId, null, null);

        return new PatientHistory(patient, records, prescriptions, documents);
    }

    public async Task<Appointment?> GetAppointmentForNoteAsync(int doctorId, int appointmentId)
    {
        var appointment = await _uow.Repository<Appointment>().GetByIdAsync(appointmentId);
        return appointment is not null && appointment.DoctorId == doctorId ? appointment : null;
    }

    // ===================== Patient side =====================

    public Task<IReadOnlyList<MedicalRecord>> GetPatientVisitsAsync(int patientId) =>
        _uow.MedicalRecords.GetByPatientAsync(patientId);

    public async Task<MedicalRecord?> GetForPatientAsync(int patientId, int recordId)
    {
        var record = await _uow.MedicalRecords.GetWithDetailsAsync(recordId);
        return record is not null && record.PatientId == patientId ? record : null;
    }

    // ===================== Documents =====================

    public async Task<ServiceResult<int>> UploadDocumentAsync(int? doctorId, string uploadedByUserId, UploadDocumentDto dto)
    {
        if (doctorId.HasValue && !await _uow.MedicalRecords.DoctorHasPatientAsync(doctorId.Value, dto.PatientId))
            return ServiceResult<int>.Fail("You can only upload files for your own patients.");

        if (dto.MedicalRecordId.HasValue)
        {
            var record = await _uow.MedicalRecords.GetByIdAsync(dto.MedicalRecordId.Value);
            if (record is null || record.PatientId != dto.PatientId)
                return ServiceResult<int>.Fail("The selected visit does not belong to this patient.");
        }

        var saved = await _storage.SaveAsync(dto.File);
        if (!saved.Succeeded)
            return ServiceResult<int>.Fail(saved.Error!);

        var document = new MedicalDocument
        {
            PatientId = dto.PatientId,
            DoctorId = doctorId,
            MedicalRecordId = dto.MedicalRecordId,
            Title = string.IsNullOrWhiteSpace(dto.Title)
                ? Path.GetFileNameWithoutExtension(dto.File.FileName)
                : dto.Title.Trim(),
            DocumentType = dto.DocumentType,
            OriginalFileName = Path.GetFileName(dto.File.FileName),
            StoredFileName = saved.Value!.StoredFileName,
            ContentType = saved.Value.ContentType,
            FileSize = saved.Value.Size,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow
        };

        try
        {
            await _uow.MedicalDocuments.AddAsync(document);
            await _uow.SaveChangesAsync();
        }
        catch
        {
            _storage.Delete(saved.Value.StoredFileName);
            throw;
        }

        return ServiceResult<int>.Ok(document.Id);
    }

    public Task<IReadOnlyList<MedicalDocument>> GetPatientDocumentsAsync(int patientId, string? filter, string? search)
    {
        DocumentType[]? types = (filter ?? "all").Trim().ToLowerInvariant() switch
        {
            "reports" => new[] { DocumentType.Report, DocumentType.Lab, DocumentType.ECG, DocumentType.Notes },
            "imaging" => new[] { DocumentType.Imaging, DocumentType.Echo },
            "discharge" => new[] { DocumentType.Discharge },
            _ => null
        };

        return _uow.MedicalDocuments.GetByPatientAsync(patientId, types, search);
    }

    public async Task<PatientRecordStats> GetPatientStatsAsync(int patientId)
    {
        var docs = await _uow.MedicalDocuments.GetByPatientAsync(patientId, null, null);
        var visits = await _uow.MedicalRecords.GetByPatientAsync(patientId);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var labs = docs.Where(d => d.DocumentType == DocumentType.Lab).ToList();
        var doctorIds = visits.Select(v => v.DoctorId)
            .Concat(docs.Where(d => d.DoctorId.HasValue).Select(d => d.DoctorId!.Value))
            .Distinct()
            .Count();

        return new PatientRecordStats(
            Reports: docs.Count,
            ReportsThisMonth: docs.Count(d => d.UploadedAt >= monthStart),
            LabResults: labs.Count,
            LabResultsThisMonth: labs.Count(d => d.UploadedAt >= monthStart),
            Visits: visits.Count,
            FirstVisitYear: visits.Count > 0 ? visits.Min(v => v.CreatedAt).Year : null,
            DoctorsWithAccess: doctorIds);
    }

    public async Task<OpenedDocument?> OpenDocumentForDoctorAsync(int doctorId, int documentId)
    {
        var doc = await _uow.MedicalDocuments.GetByIdAsync(documentId);
        if (doc is null || !await _uow.MedicalRecords.DoctorHasPatientAsync(doctorId, doc.PatientId))
            return null;

        return Open(doc);
    }

    public async Task<OpenedDocument?> OpenDocumentForPatientAsync(int patientId, int documentId)
    {
        var doc = await _uow.MedicalDocuments.GetByIdAsync(documentId);
        return doc is not null && doc.PatientId == patientId ? Open(doc) : null;
    }

    private OpenedDocument? Open(MedicalDocument doc)
    {
        var stream = _storage.OpenRead(doc.StoredFileName);
        return stream is null ? null : new OpenedDocument(doc, stream);
    }
}
