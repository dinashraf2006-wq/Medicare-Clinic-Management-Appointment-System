using MediCare.Data;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Web.Demo;

internal static class LocalSqliteUpgrade
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        if (!await db.Database.SqlQueryRaw<int>(
            "SELECT 1 AS Value FROM pragma_table_info('Doctors') WHERE name = 'HasCustomSchedule'").AnyAsync())
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Doctors ADD COLUMN HasCustomSchedule INTEGER NOT NULL DEFAULT 0");

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS DoctorWorkingHours (
                DoctorId INTEGER NOT NULL REFERENCES Doctors(Id) ON DELETE CASCADE,
                Day INTEGER NOT NULL,
                Start TEXT NOT NULL,
                End TEXT NOT NULL,
                PRIMARY KEY (DoctorId, Day)
            );
            CREATE TABLE IF NOT EXISTS DoctorVacationDays (
                DoctorId INTEGER NOT NULL REFERENCES Doctors(Id) ON DELETE CASCADE,
                Date TEXT NOT NULL,
                PRIMARY KEY (DoctorId, Date)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Appointments_Patient_Slot_Active
                ON Appointments (PatientId, AppointmentDate, StartTime)
                WHERE Status IN ('Pending', 'Confirmed');
            """);
        await transaction.CommitAsync();
    }
}
