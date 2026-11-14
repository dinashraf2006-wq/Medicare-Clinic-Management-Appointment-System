using System.Security.Claims;
using MediCare.Services.MedicalRecords;
using MediCare.Services.Storage;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Web.Controllers.MedicalRecords;

/// <summary>Common helpers for the Medical Records & Prescriptions controllers.</summary>
public abstract class MedicalRecordsControllerBase : Controller
{
    protected readonly IProfileLookupService Profiles;

    protected MedicalRecordsControllerBase(IProfileLookupService profiles) => Profiles = profiles;

    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    protected Task<int?> CurrentDoctorIdAsync() => Profiles.GetDoctorIdAsync(CurrentUserId);

    protected Task<int?> CurrentPatientIdAsync() => Profiles.GetPatientIdAsync(CurrentUserId);

    protected static FileUpload ToUpload(IFormFile file) => new()
    {
        FileName = file.FileName,
        ContentType = file.ContentType,
        Length = file.Length,
        Content = file.OpenReadStream()
    };

    /// <summary>Message shown in the "File uploaded" / "Saved" dialog after a redirect.</summary>
    protected void ShowSuccess(string title, string message)
    {
        TempData["SuccessTitle"] = title;
        TempData["SuccessMessage"] = message;
    }

    protected void ShowError(string message) => TempData["ErrorMessage"] = message;

    /// <summary>The logged-in user is in the right role but has no Doctor/Patient profile row.</summary>
    protected IActionResult MissingProfile() =>
        View("~/Views/MedicalRecordsShared/MissingProfile.cshtml");
}
