using System.ComponentModel.DataAnnotations;
using MediCare.Data;
using MediCare.Data.Entities;
using MediCare.Web.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDoctorsController(ApplicationDbContext db, AccountProvisioner accounts) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadSpecializationsAsync();
        return View(new CreateDoctorInput());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDoctorInput input)
    {
        if (ModelState.IsValid && !await db.Specializations.AnyAsync(s => s.Id == input.SpecializationId))
            ModelState.AddModelError(nameof(input.SpecializationId), "Select a valid specialization.");

        if (ModelState.IsValid)
        {
            var email = input.Email.Trim();
            var result = await accounts.CreateDoctorAsync(new ApplicationUser
            {
                UserName = email, Email = email, FullName = input.FullName.Trim()
            }, input.Password, new Doctor
            {
                City = input.City.Trim(),
                SpecializationId = input.SpecializationId,
                ConsultationFee = input.ConsultationFee,
                Bio = input.Bio?.Trim()
            });
            if (result.Succeeded) return RedirectToAction("Users", "Admin");

            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        }

        await LoadSpecializationsAsync();
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSpecialization(string? name)
    {
        var value = name?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 100)
        {
            TempData["SpecializationMessage"] = "Enter a specialization name of up to 100 characters.";
            return RedirectToAction(nameof(Create));
        }

        var normalized = value.ToUpperInvariant();
        if (!await db.Specializations.AnyAsync(s => s.Name.ToUpper() == normalized))
        {
            db.Specializations.Add(new Specialization { Name = value });
            await db.SaveChangesAsync();
        }
        TempData["SpecializationMessage"] = "Specialization is available in the doctor form.";
        return RedirectToAction(nameof(Create));
    }

    private async Task LoadSpecializationsAsync() => ViewBag.Specializations = new SelectList(
        await db.Specializations.OrderBy(s => s.Name).ToListAsync(), nameof(Specialization.Id), nameof(Specialization.Name));
}

public sealed class CreateDoctorInput
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Display(Name = "Specialization"), Range(1, int.MaxValue)]
    public int SpecializationId { get; set; }

    [Display(Name = "Consultation fee"), Range(typeof(decimal), "0", "99999999.99")]
    public decimal ConsultationFee { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }
}
