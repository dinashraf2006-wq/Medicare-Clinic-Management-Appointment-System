using Microsoft.Extensions.DependencyInjection;

namespace MediCare.Services.Doctors
{
    public static class SpecializationServiceCollectionExtensions
    {
        public static IServiceCollection AddDoctorServices(this IServiceCollection services)
        {
            services.AddScoped<IDoctorService, DoctorService>();
            return services;
        }
    }
}