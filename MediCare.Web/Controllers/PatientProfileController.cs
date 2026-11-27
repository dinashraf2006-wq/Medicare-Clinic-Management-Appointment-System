using System.ComponentModel.DataAnnotations;
using MediCare.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers;

[Authorize(Roles = "Patient")]
[Route("Patient/Profile")]
public class PatientProfileController(UserManager<ApplicationUser> users, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();

        ViewData["Email"] = user.Email;
        return View(new PatientProfileInput { FullName = user.FullName, PhoneNumber = user.PhoneNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PatientProfileInput input)
    {
        if (environment.IsEnvironment("Demo")) return NotFound();
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        ViewData["Email"] = user.Email;

        if (!ModelState.IsValid) return View(input);

        user.FullName = input.FullName.Trim();
        var phone = input.PhoneNumber?.Trim();
        if (user.PhoneNumber != phone)
        {
            user.PhoneNumber = phone;
            user.PhoneNumberConfirmed = false;
        }

        var result = await users.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["ProfileSaved"] = true;
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(input);
    }
}

public class PatientProfileInput
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }
}
