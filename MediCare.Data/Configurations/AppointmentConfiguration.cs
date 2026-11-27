using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCare.Data.Configurations;

// Member 3 - Appointment Booking.
public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> b)
    {
        b.Property(a => a.Status).HasMaxLength(30);
        b.Property(a => a.AppointmentDate).HasColumnType("date");
        b.Property(a => a.PatientNotes).HasMaxLength(500);

        b.HasOne(a => a.Doctor).WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Patient).WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);

        // Last line of defence against double booking: the database itself refuses two active
        // appointments for the same doctor at the same date + time (two patients clicking "Confirm" together).
        b.HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.StartTime })
            .IsUnique()
            .HasFilter("[Status] IN ('Pending', 'Confirmed')")
            .HasDatabaseName("UX_Appointments_Doctor_Slot_Active");

        b.HasIndex(a => new { a.PatientId, a.AppointmentDate });
        b.HasIndex(a => new { a.PatientId, a.AppointmentDate, a.StartTime })
            .IsUnique()
            .HasFilter("[Status] IN ('Pending', 'Confirmed')")
            .HasDatabaseName("UX_Appointments_Patient_Slot_Active");
    }
}
