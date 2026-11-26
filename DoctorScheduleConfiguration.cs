using MediCare.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCare.Data.Configurations;

public class WorkingHourConfiguration : IEntityTypeConfiguration<WorkingHour>
{
    public void Configure(EntityTypeBuilder<WorkingHour> b)
    {
        b.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.DoctorId, x.DayOfWeek, x.StartTime }).IsUnique();
    }
}

public class DoctorVacationConfiguration : IEntityTypeConfiguration<DoctorVacation>
{
    public void Configure(EntityTypeBuilder<DoctorVacation> b)
    {
        b.Property(x => x.Reason).HasMaxLength(300);
        b.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.DoctorId, x.StartDate, x.EndDate });
    }
}
