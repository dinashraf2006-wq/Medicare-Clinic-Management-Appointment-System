using Microsoft.AspNetCore.Identity;

namespace MediCare.Data.Entities;

// SHARED (owned by Member 1 - Authentication).
// Minimal version so the Medical Records module can compile and run.
// When merging, keep Member 1's version and make sure it still has FullName.
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
