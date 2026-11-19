using Microsoft.Extensions.DependencyInjection;

namespace MediCare.Services.Doctors
{
    public static class DoctorsServiceCollectionExtensions
    {
        public static IServiceCollection AddDoctorServices(this IServiceCollection services)
        {
            services.AddScoped<IDoctorService, DoctorService>();
            return services;
        }
    }
}