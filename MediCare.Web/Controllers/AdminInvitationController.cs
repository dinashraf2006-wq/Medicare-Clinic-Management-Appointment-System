using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediCare.Web.Models;

namespace MediCare.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminInvitationController : Controller
{
    public IActionResult CreateAdmin()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Redirect to Register with a hidden field for Admin role
        TempData["AdminEmail"] = model.Email;
        TempData["AdminFullName"] = model.FullName;
        TempData["AdminPhone"] = model.PhoneNumber;
        TempData["AdminPassword"] = model.Password;
        TempData["CreateAsAdmin"] = true;

        return RedirectToAction("Register", "Account");
    }
}
