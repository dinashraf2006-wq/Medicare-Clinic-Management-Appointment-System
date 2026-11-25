using Microsoft.AspNetCore.Identity;
using MediCare.Data.Entities;

namespace MediCare.Web.DevTools;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Ensure Admin role exists
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Find or create admin
        var admin = await userManager.FindByEmailAsync("admin@demo.medicare");
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@demo.medicare",
                FullName = "System Administrator",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Demo@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                Console.WriteLine("✅ Admin account created: admin@demo.medicare / Demo@12345");
            }
        }
        else
        {
            // Ensure admin has Admin role
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            // Reset password
            var token = await userManager.GeneratePasswordResetTokenAsync(admin);
            await userManager.ResetPasswordAsync(admin, token, "Demo@12345");
            Console.WriteLine("✅ Admin account password reset: admin@demo.medicare / Demo@12345");
        }
    }
}
