namespace MediCare.Data.Repositories;

// SHARED: when merging, add each member's repository property here.
public interface IUnitOfWork : IDisposable
{
    // Member 5 - Medical Records & Prescriptions
    IMedicalRecordRepository MedicalRecords { get; }
    IMedicalDocumentRepository MedicalDocuments { get; }
    IPrescriptionRepository Prescriptions { get; }

    // Generic access to any other table (Doctors, Patients, ...)
    IGenericRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync();
}
