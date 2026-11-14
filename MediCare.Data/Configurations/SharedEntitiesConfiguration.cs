using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCare.Data.Configurations;

// SHARED: minimal settings for teammates' tables. Their own configuration can replace this file.

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> b)
    {
        b.Property(d => d.ConsultationFee).HasPrecision(10, 2);
        b.HasIndex(d => d.UserId).IsUnique();
        b.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(d => d.Specialization).WithMany(s => s.Doctors)
            .HasForeignKey(d => d.SpecializationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> b)
    {
        b.HasIndex(p => p.UserId).IsUnique();
        b.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> b)
    {
        b.Property(a => a.Status).HasMaxLength(30);
        b.HasOne(a => a.Doctor).WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Patient).WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SpecializationConfiguration : IEntityTypeConfiguration<Specialization>
{
    public void Configure(EntityTypeBuilder<Specialization> b)
    {
        b.Property(s => s.Name).HasMaxLength(100).IsRequired();
    }
}
