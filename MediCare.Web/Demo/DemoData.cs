using MediCare.Data;
using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Web.Demo;

internal static class DemoData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.Doctors.AnyAsync()) return;

        var cardiology = new Specialization { Name = "Cardiology", Description = "Sample heart and vascular care" };
        var dermatology = new Specialization { Name = "Dermatology", Description = "Sample skin care" };
        var doctor = new Doctor
        {
            User = Profile("demo-cardiologist", "Dr. Noor (Demo)"),
            Specialization = cardiology,
            City = "Cairo",
            ConsultationFee = 450,
            Bio = "Fictional cardiologist for testing appointment booking."
        };
        db.Doctors.AddRange(doctor, new Doctor
        {
            User = Profile("demo-dermatologist", "Dr. Samir (Demo)"),
            Specialization = dermatology,
            City = "Alexandria",
            ConsultationFee = 350,
            Bio = "Fictional dermatologist for testing appointment booking."
        });

        var nextClinicDay = DateTime.UtcNow.Date.AddDays(2);
        if (nextClinicDay.DayOfWeek == DayOfWeek.Friday) nextClinicDay = nextClinicDay.AddDays(1);

        db.Appointments.Add(new Appointment
        {
            Doctor = doctor,
            Patient = new Patient { User = Profile("demo-booking", "Sample patient") },
            AppointmentDate = nextClinicDay,
            StartTime = TimeSpan.FromHours(10),
            Status = AppointmentStatus.Confirmed
        });
        await db.SaveChangesAsync();
    }

    private static ApplicationUser Profile(string handle, string name) => new()
    {
        UserName = handle,
        NormalizedUserName = handle.ToUpperInvariant(),
        Email = $"{handle}@medicare.example",
        NormalizedEmail = $"{handle}@medicare.example".ToUpperInvariant(),
        FullName = name
    };
}
