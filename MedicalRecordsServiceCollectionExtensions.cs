using MediCare.Services.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace MediCare.Services.MedicalRecords;

public static class MedicalRecordsServiceCollectionExtensions
{
    /// <summary>Registers everything the Medical Records & Prescriptions module needs (one line in Program.cs).</summary>
    public static IServiceCollection AddMedicalRecordsModule(this IServiceCollection services, string uploadsRootPath)
    {
        services.AddSingleton(new FileStorageOptions { RootPath = uploadsRootPath });
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        services.AddScoped<IProfileLookupService, ProfileLookupService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        return services;
    }
}
