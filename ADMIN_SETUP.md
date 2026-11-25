# Admin Account Setup for Production

## Overview
The admin dashboard is secured with role-based access control (RBAC). Only users with the "Admin" role can access `/Admin/Dashboard`.

## Security Features
- ✅ Public registration page only shows "Patient" and "Doctor" options
- ✅ Admin role is NOT selectable by regular users
- ✅ Admin dashboard is protected with `[Authorize(Roles = "Admin")]`
- ✅ Only existing admins can create new admin accounts

## Creating the First Admin in Production

### Option 1: Direct Database Insertion (Recommended for Production)

Run this SQL query in your production database to create the first admin:

```sql
-- Insert admin user (replace with actual values)
INSERT INTO AspNetUsers (Id, UserName, Email, EmailConfirmed, FullName, PhoneNumber, SecurityStamp, ConcurrencyStamp)
VALUES (
    NEWID(),
    'admin_username',  -- e.g., 'admin'
    'admin@yourdomain.com',
    1,
    'System Administrator',
    '+201xxxxxxxxx',
    NEWID(),
    NEWID()
)

-- Get the user ID you just inserted
DECLARE @UserId UNIQUEIDENTIFIER
SELECT @UserId = Id FROM AspNetUsers WHERE Email = 'admin@yourdomain.com'

-- Insert password hash (use a real hash in production)
INSERT INTO AspNetUserLogins (LoginProvider, ProviderKey, ProviderDisplayName, UserId)
VALUES ('Local', @UserId, 'Local', @UserId)

-- Create Admin role if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID())
END

-- Get the Admin role ID
DECLARE @RoleId UNIQUEIDENTIFIER
SELECT @RoleId = Id FROM AspNetRoles WHERE Name = 'Admin'

-- Assign admin role to the user
INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES (@UserId, @RoleId)
```

**Important:** For the password hash, use ASP.NET Core's password hasher:
```csharp
var hasher = new PasswordHasher<ApplicationUser>();
var hashedPassword = hasher.HashPassword(user, "YourSecurePassword123!");
```

### Option 2: One-Time Seeder Script

Create a temporary seeder that runs once in production:

```csharp
// In Program.cs, add this before app.Run():
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Create Admin role
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Create first admin (replace with your credentials)
    var adminEmail = "admin@yourdomain.com";
    if (!await userManager.Users.AnyAsync(u => u.Email == adminEmail))
    {
        var adminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = adminEmail,
            FullName = "System Administrator",
            PhoneNumber = "+201xxxxxxxxx",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, "YourSecurePassword123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
```

**⚠️ IMPORTANT:** After running this once, REMOVE the code from production to prevent security risks.

## Creating Additional Admins (After First Admin Exists)

Once you have the first admin account:

1. **Log in** with your admin credentials at `/Account/Login`
2. **Go to** `/Admin/Users` (Admin Dashboard → Users)
3. **Click** the green "Create Admin" button
4. **Fill in** the new admin's details (name, email, phone, password)
5. **Click** "Create Admin Account"

The new admin will be created with the Admin role and can immediately access the admin dashboard.

## Removing the AdminInvitationController (Optional)

If you don't want the "Create Admin" feature in production, you can:
1. Remove `AdminInvitationController.cs`
2. Remove the "Create Admin" button from `Views/Admin/Users.cshtml`
3. Manually manage admin roles through the database

## Security Checklist for Production

- [ ] Remove any development-only admin creation code
- [ ] Ensure the Admin role is NOT selectable in public registration
- [ ] Use strong passwords for admin accounts
- [ ] Enable HTTPS in production
- [ ] Consider implementing 2FA for admin accounts
- [ ] Regularly audit admin access logs
- [ ] Limit admin account creation to trusted personnel only
