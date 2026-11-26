using MediCare.Data.Entities;
using MediCare.Data.Repositories;

namespace MediCare.Services.MedicalRecords;

/// <summary>Turns the logged-in Identity user id into a Doctor / Patient id.</summary>
public interface IProfileLookupService
{
    Task<int?> GetDoctorIdAsync(string userId);
    Task<int?> GetPatientIdAsync(string userId);
}

public class ProfileLookupService : IProfileLookupService
{
    private readonly IUnitOfWork _uow;

    public ProfileLookupService(IUnitOfWork uow) => _uow = uow;

    public async Task<int?> GetDoctorIdAsync(string userId) =>
        (await _uow.Repository<Doctor>().FirstOrDefaultAsync(d => d.UserId == userId))?.Id;

    public async Task<int?> GetPatientIdAsync(string userId) =>
        (await _uow.Repository<Patient>().FirstOrDefaultAsync(p => p.UserId == userId))?.Id;
}
