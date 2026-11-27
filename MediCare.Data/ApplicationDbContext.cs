using MediCare.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Data;

// SHARED FILE. When merging with teammates, keep one copy of this class and
// add each member's DbSet lines. Entity settings live in the Configurations folder,
// so nobody has to edit OnModelCreating.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Shared tables (from the ERD)
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<DoctorWorkingHours> DoctorWorkingHours => Set<DoctorWorkingHours>();
    public DbSet<DoctorVacationDay> DoctorVacationDays => Set<DoctorVacationDay>();

    // Member 5 - Medical Records & Prescriptions
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<MedicalDocument> MedicalDocuments => Set<MedicalDocument>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<DoctorWorkingHours>().HasKey(hours => new { hours.DoctorId, hours.Day });
        builder.Entity<DoctorWorkingHours>().HasOne<Doctor>().WithMany().HasForeignKey(hours => hours.DoctorId);
        builder.Entity<DoctorVacationDay>().HasKey(day => new { day.DoctorId, day.Date });
        builder.Entity<DoctorVacationDay>().HasOne<Doctor>().WithMany().HasForeignKey(day => day.DoctorId);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            builder.Entity<Appointment>().Property(appointment => appointment.StartTime).HasConversion<long>();
        }
    }
}
