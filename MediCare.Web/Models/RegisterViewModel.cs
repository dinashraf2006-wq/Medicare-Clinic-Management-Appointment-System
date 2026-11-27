using Microsoft.AspNetCore.Mvc.Rendering;

namespace MediCare.Web.Models
{
    public class RegisterViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserRole { get; set; } = "Patient";

        public int SpecializationId { get; set; }
        public string City { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }

        public IEnumerable<SelectListItem> Specializations { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Cities { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
