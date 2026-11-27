using MediCare.Data;
using MediCare.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Web.Accounts;

public sealed class AccountProvisioner(ApplicationDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
{
    public Task<IdentityResult> CreatePatientAsync(ApplicationUser user, string? password) =>
        CreateAsync(user, password, "Patient");

    public Task<IdentityResult> CreateAdminAsync(ApplicationUser user, string password) =>
        CreateAsync(user, password, "Admin");

    public Task<IdentityResult> CreateDoctorAsync(ApplicationUser user, string password, Doctor profile) =>
        CreateAsync(user, password, "Doctor", profile);

    public async Task EnsurePatientProfileAsync(ApplicationUser user)
    {
        if (await db.Patients.AnyAsync(patient => patient.UserId == user.Id)) return;

        var profile = new Patient { UserId = user.Id };
        db.Patients.Add(profile);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.Entry(profile).State = EntityState.Detached;
            if (!await db.Patients.AnyAsync(patient => patient.UserId == user.Id)) throw;
        }
    }

    private async Task<IdentityResult> CreateAsync(ApplicationUser user, string? password, string role, Doctor? doctor = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!await roles.RoleExistsAsync(role))
        {
            var roleResult = await roles.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded) return roleResult;
        }

        var userResult = password is null ? await users.CreateAsync(user) : await users.CreateAsync(user, password);
        if (!userResult.Succeeded) return userResult;

        var membershipResult = await users.AddToRoleAsync(user, role);
        if (!membershipResult.Succeeded) return membershipResult;

        if (role == "Patient")
        {
            db.Patients.Add(new Patient { UserId = user.Id });
        }
        if (doctor is not null)
        {
            doctor.UserId = user.Id;
            db.Doctors.Add(doctor);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return IdentityResult.Success;
    }
}
