using Microsoft.Extensions.DependencyInjection;

namespace MediCare.Services.Admin;

public static class AdminServiceCollectionExtensions
{
    public static IServiceCollection AddAdminServices(this IServiceCollection services)
    {
        services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
        return services;
    }
}
