using MediCare.Web.Models.Specialties;

namespace MediCare.Web.Models.Home
{
    public class HomeViewModel
    {
        public List<SpecialtyCardItem> FeaturedSpecialties { get; set; } = new();
        public List<DoctorCardItem> TopDoctors { get; set; } = new();

        public List<string> AvailableCities { get; set; } = new();
    }

    public class DoctorCardItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public string? ProfileImageUrl { get; set; }

    }
}
