using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCare.Data.Configurations;

// Member 5 - Medical Records & Prescriptions.
// All foreign keys use Restrict (medical history must never disappear because a related row was deleted,
// and SQL Server does not allow multiple cascade paths). Only prescription items cascade with their prescription.

public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> b)
    {
        b.Property(r => r.Symptoms).HasMaxLength(2000).IsRequired();
        b.Property(r => r.Diagnosis).HasMaxLength(1000).IsRequired();
        b.Property(r => r.TreatmentNotes).HasMaxLength(4000);

        b.HasOne(r => r.Patient).WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Doctor).WithMany().HasForeignKey(r => r.DoctorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Appointment).WithMany().HasForeignKey(r => r.AppointmentId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(r => new { r.PatientId, r.CreatedAt });
    }
}

public class MedicalDocumentConfiguration : IEntityTypeConfiguration<MedicalDocument>
{
    public void Configure(EntityTypeBuilder<MedicalDocument> b)
    {
        b.Property(d => d.Title).HasMaxLength(200).IsRequired();
        b.Property(d => d.OriginalFileName).HasMaxLength(255).IsRequired();
        b.Property(d => d.StoredFileName).HasMaxLength(100).IsRequired();
        b.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        b.Property(d => d.UploadedByUserId).HasMaxLength(450).IsRequired();

        b.HasOne(d => d.Patient).WithMany().HasForeignKey(d => d.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(d => d.Doctor).WithMany().HasForeignKey(d => d.DoctorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(d => d.MedicalRecord).WithMany(r => r.Documents)
            .HasForeignKey(d => d.MedicalRecordId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(d => d.StoredFileName).IsUnique();
        b.HasIndex(d => new { d.PatientId, d.UploadedAt });
    }
}

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> b)
    {
        b.Property(p => p.Notes).HasMaxLength(1000);
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        b.HasOne(p => p.Patient).WithMany().HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Doctor).WithMany().HasForeignKey(p => p.DoctorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.MedicalRecord).WithMany(r => r.Prescriptions)
            .HasForeignKey(p => p.MedicalRecordId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(p => p.Items).WithOne(i => i.Prescription)
            .HasForeignKey(i => i.PrescriptionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> b)
    {
        b.Property(i => i.MedicationName).HasMaxLength(150).IsRequired();
        b.Property(i => i.Indication).HasMaxLength(150);
        b.Property(i => i.Dose).HasMaxLength(100).IsRequired();
        b.Property(i => i.Timing).HasMaxLength(100);
        b.Property(i => i.Instructions).HasMaxLength(1000);
        b.Property(i => i.Frequency).HasConversion<string>().HasMaxLength(30);
    }
}
