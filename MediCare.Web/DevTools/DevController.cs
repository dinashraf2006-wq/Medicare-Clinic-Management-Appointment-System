using MediCare.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.DevTools;

/// <summary>
/// DEVELOPMENT ONLY: one-click sign in as a demo doctor or patient, until Member 1's login page exists.
/// Returns 404 outside the Development environment.
/// </summary>
[Route("dev")]
public class DevController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DevController(IWebHostEnvironment env, SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _env = env;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("")]
    public IActionResult Index(string? returnUrl = null)
    {
        if (!_env.IsDevelopment()) return NotFound();
        ViewData["ReturnUrl"] = returnUrl;
        return View("~/DevTools/Index.cshtml", MedicalRecordsDevSeeder.DemoUsers);
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string? returnUrl = null)
    {
        if (!_env.IsDevelopment()) return NotFound();

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return RedirectToAction(nameof(Index));

        await _signInManager.SignInAsync(user, isPersistent: false);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return await _userManager.IsInRoleAsync(user, "Doctor")
            ? Redirect("/Doctor/MedicalRecords")
            : Redirect("/Patient/MedicalRecords");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Index));
    }
}
