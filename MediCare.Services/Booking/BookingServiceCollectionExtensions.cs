using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MediCare.Services.Booking;

public static class BookingServiceCollectionExtensions
{
    /// <summary>Registers everything the Appointment Booking module needs (one line in Program.cs).</summary>
    public static IServiceCollection AddAppointmentBookingModule(this IServiceCollection services, Action<BookingOptions>? configure = null)
    {
        var options = new BookingOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.TryAddSingleton(TimeProvider.System);
        // Member 4 can register their own IDoctorAvailabilityProvider BEFORE this call to replace the default hours.
        services.TryAddScoped<IDoctorAvailabilityProvider, DefaultDoctorAvailabilityProvider>();
        services.AddScoped<IBookingService, BookingService>();
        return services;
    }
}
