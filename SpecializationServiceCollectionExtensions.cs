using Microsoft.Extensions.DependencyInjection;

namespace MediCare.Services.Specializations
{
    public static class SpecializationServiceCollectionExtensions
    {
        public static IServiceCollection AddSpecializationServices(this IServiceCollection services)
        {
            services.AddScoped<ISpecializationService, SpecializationService>();
            return services;
        }
    }
}