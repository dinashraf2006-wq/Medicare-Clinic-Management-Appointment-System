using MediCare.Data.Entities;
using MediCare.Web.Accounts;
using MediCare.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MediCare.Web.Controllers;

public class AccountController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    AccountProvisioner accounts,
    IWebHostEnvironment environment) : Controller
{
    private bool IsDemo => environment.IsEnvironment("Demo");

    [HttpGet]
    public IActionResult Register(string? returnUrl) => IsDemo
        ? View("StartDemo", returnUrl)
        : View(new RegisterViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (IsDemo) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber?.Trim()
        };

        var result = await accounts.CreatePatientAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await signIn.SignInAsync(user, isPersistent: false);
        return ContinueToBooking(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account")]
    public async Task<IActionResult> StartDemo(string? returnUrl)
    {
        if (!IsDemo) return NotFound();
        if (User.IsInRole("Patient")) return ContinueToBooking(returnUrl);

        var id = Guid.NewGuid().ToString("N");
        var email = $"visitor-{id}@medicare.example";
        var user = new ApplicationUser { UserName = email, Email = email, FullName = $"Demo patient {id[..8]}" };
        var result = await accounts.CreatePatientAsync(user, password: null);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Couldn't start a demo session. Please try again.");
            return View("StartDemo", returnUrl);
        }

        await signIn.SignInAsync(user, isPersistent: false);
        return ContinueToBooking(returnUrl);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl) => IsDemo
        ? RedirectToAction(nameof(Register), new { returnUrl })
        : View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (IsDemo) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var login = model.EmailOrPhone.Trim();
        var user = await users.FindByEmailAsync(login) ?? await users.FindByNameAsync(login);
        if (user is not null)
        {
            var result = await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                if (await users.IsInRoleAsync(user, "Admin")) return RedirectToAction("Dashboard", "Admin");
                if (await users.IsInRoleAsync(user, "Doctor")) return Redirect("/Doctor/Schedule");
                if (await users.IsInRoleAsync(user, "Patient"))
                {
                    await accounts.EnsurePatientProfileAsync(user);
                    return ContinueToBooking(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
        }

        ModelState.AddModelError(string.Empty, "Sign-in failed. Check your credentials or try again later.");
        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied() => StatusCode(StatusCodes.Status403Forbidden);

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult LogoutConfirmation() => RedirectToAction("Index", "Home");

    private IActionResult ContinueToBooking(string? returnUrl) => Url.IsLocalUrl(returnUrl)
        ? LocalRedirect(returnUrl!)
        : RedirectToAction("Index", "PatientBooking");
}
