using System.ComponentModel.DataAnnotations;

namespace MediCare.Web.Models
{
    public class LoginViewModel
    {
        [Required, StringLength(254)]
        public string EmailOrPhone { get; set; } = string.Empty;
        [Required, StringLength(128), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
