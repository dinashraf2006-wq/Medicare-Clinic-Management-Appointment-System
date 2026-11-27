using MediCare.Data.Entities;
using MediCare.Web.Accounts;
using MediCare.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminInvitationController(AccountProvisioner accounts) : Controller
{
    public IActionResult CreateAdmin() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var result = await accounts.CreateAdminAsync(new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim()
        }, model.Password);

        if (result.Succeeded) return RedirectToAction("Users", "Admin");

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }
}
