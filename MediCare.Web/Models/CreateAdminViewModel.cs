using System.ComponentModel.DataAnnotations;

namespace MediCare.Web.Models;

public class CreateAdminViewModel
{
    [Required]
    [EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 10), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
