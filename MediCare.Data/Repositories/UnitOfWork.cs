namespace MediCare.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    private IMedicalRecordRepository? _medicalRecords;
    private IMedicalDocumentRepository? _medicalDocuments;
    private IPrescriptionRepository? _prescriptions;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IMedicalRecordRepository MedicalRecords =>
        _medicalRecords ??= new MedicalRecordRepository(_context);

    public IMedicalDocumentRepository MedicalDocuments =>
        _medicalDocuments ??= new MedicalDocumentRepository(_context);

    public IPrescriptionRepository Prescriptions =>
        _prescriptions ??= new PrescriptionRepository(_context);

    public IGenericRepository<T> Repository<T>() where T : class
    {
        if (!_repositories.TryGetValue(typeof(T), out var repo))
        {
            repo = new GenericRepository<T>(_context);
            _repositories[typeof(T)] = repo;
        }
        return (IGenericRepository<T>)repo;
    }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
