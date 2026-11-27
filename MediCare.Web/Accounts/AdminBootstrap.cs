using MediCare.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace MediCare.Web.Accounts;

internal static class AdminBootstrap
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["BootstrapAdmin:Email"]?.Trim();
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(password)) return;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Set both bootstrap administrator settings.");

        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await users.IsInRoleAsync(existing, "Admin"))
                throw new InvalidOperationException("The bootstrap administrator email already belongs to a different account.");
            return;
        }

        var accounts = scope.ServiceProvider.GetRequiredService<AccountProvisioner>();
        var result = await accounts.CreateAdminAsync(new ApplicationUser
        {
            UserName = email, Email = email, FullName = "System Administrator"
        }, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Administrator provisioning failed: " + string.Join(", ", result.Errors.Select(error => error.Description)));
    }
}
