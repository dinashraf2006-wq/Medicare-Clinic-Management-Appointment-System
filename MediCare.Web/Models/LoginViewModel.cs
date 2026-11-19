namespace MediCare.Web.Models
{
    public class LoginViewModel
    {
        public string EmailOrPhone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public string UserRole { get; set; } = "Patient";
    }
}
