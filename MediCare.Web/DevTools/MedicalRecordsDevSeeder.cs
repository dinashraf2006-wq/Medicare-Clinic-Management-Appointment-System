using MediCare.Data;
using MediCare.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Web.DevTools;

public record DemoUser(string Email, string Name, string Role);

/// <summary>
/// DEVELOPMENT ONLY. Creates demo doctors, patients, appointments, a visit note and a prescription
/// so the Medical Records module can be tested before the other modules exist.
/// Safe to delete once the team has real registration and booking.
/// </summary>
public static class MedicalRecordsDevSeeder
{
    public const string DemoPassword = "Demo@12345";

    public static readonly DemoUser[] DemoUsers =
    {
        new DemoUser("omar.khaled@demo.medicare", "Dr. Omar Khaled", "Doctor"),
        new DemoUser("ahmed.hassan@demo.medicare", "Dr. Ahmed Hassan", "Doctor"),
        new DemoUser("sarah.mohamed@demo.medicare", "Sarah Mohamed", "Patient"),
        new DemoUser("ahmed.ali@demo.medicare", "Ahmed Ali", "Patient"),
        new DemoUser("laila.fathy@demo.medicare", "Laila Fathy", "Patient")
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(MedicalRecordsDevSeeder));

        if (db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            // No migrations yet (the team hasn't created them): build the tables straight from the model
            // so the app can run with F5. Once the team adds migrations, delete the MediCareDb database once
            // and the code above takes over.
            logger.LogWarning("No EF migrations found - creating the development database with EnsureCreated().");
            await db.Database.EnsureCreatedAsync();
        }

        if (await db.Users.AnyAsync(u => u.Email == DemoUsers[0].Email))
            return; // already seeded

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { "Admin", "Doctor", "Patient" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        var users = new Dictionary<string, ApplicationUser>();
        foreach (var (email, name, role) in DemoUsers)
        {
            var user = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Could not create demo user {Email}: {Errors}", email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
            await userManager.AddToRoleAsync(user, role);
            users[email] = user;
        }

        var cardiology = await db.Specializations.FirstOrDefaultAsync(s => s.Name == "Cardiology")
                         ?? new Specialization { Name = "Cardiology", Description = "Heart & vascular care" };

        var omar = new Doctor { User = users[DemoUsers[0].Email], Specialization = cardiology, ConsultationFee = 450 };
        var ahmedDr = new Doctor { User = users[DemoUsers[1].Email], Specialization = cardiology, ConsultationFee = 450 };
        var sarah = new Patient { User = users[DemoUsers[2].Email], Allergies = "Penicillin", MedicalHistory = "Hypertension" };
        var ahmedAli = new Patient { User = users[DemoUsers[3].Email] };
        var laila = new Patient { User = users[DemoUsers[4].Email] };
        db.AddRange(omar, ahmedDr, sarah, ahmedAli, laila);

        var today = DateTime.UtcNow.Date;
        var sarahVisit = new Appointment { Doctor = omar, Patient = sarah, AppointmentDate = today.AddDays(-12), StartTime = new TimeSpan(9, 0, 0), Status = "Completed" };
        db.Appointments.AddRange(
            sarahVisit,
            new Appointment { Doctor = omar, Patient = ahmedAli, AppointmentDate = today, StartTime = new TimeSpan(10, 0, 0), Status = "Confirmed" },
            new Appointment { Doctor = omar, Patient = laila, AppointmentDate = today.AddDays(1), StartTime = new TimeSpan(14, 30, 0), Status = "Confirmed" },
            new Appointment { Doctor = ahmedDr, Patient = sarah, AppointmentDate = today.AddDays(1), StartTime = new TimeSpan(10, 0, 0), Status = "Confirmed" });

        var record = new MedicalRecord
        {
            Patient = sarah,
            Doctor = omar,
            Appointment = sarahVisit,
            Symptoms = "Chest discomfort on exertion, occasional headaches.",
            Diagnosis = "Essential hypertension",
            TreatmentNotes = "BP 150/95. Start Amlodipine 5mg daily. Low-salt diet. Follow up in 4 weeks with home BP log.",
            CreatedAt = DateTime.UtcNow.AddDays(-12)
        };
        db.MedicalRecords.Add(record);

        db.Prescriptions.Add(new Prescription
        {
            Patient = sarah,
            Doctor = omar,
            MedicalRecord = record,
            Status = PrescriptionStatus.Sent,
            CreatedAt = DateTime.UtcNow.AddDays(-12),
            SentAt = DateTime.UtcNow.AddDays(-12),
            Items =
            {
                new PrescriptionItem
                {
                    MedicationName = "Amlodipine 5mg", Indication = "Blood pressure", Dose = "1 tablet",
                    Frequency = MedicationFrequency.OnceDaily, Timing = "after breakfast", DurationDays = 30,
                    Instructions = "Take 1 tablet after breakfast\nAvoid grapefruit juice\nDon't stop without asking your doctor"
                },
                new PrescriptionItem
                {
                    MedicationName = "Atorvastatin 20mg", Indication = "Cholesterol", Dose = "1 tablet",
                    Frequency = MedicationFrequency.OnceDaily, Timing = "before sleep", DurationDays = 14
                }
            }
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Medical Records demo data created. Open /dev to sign in as a demo user.");
    }
}
