using System.ComponentModel.DataAnnotations;

namespace MediCare.Web.Models;

public class RegisterViewModel
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;
    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }
    [Required, StringLength(128, MinimumLength = 10), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}
