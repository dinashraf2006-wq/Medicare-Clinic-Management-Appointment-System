namespace MediCare.Web.Models.Home
{
    public class HomeViewModel
    {
        public List<SpecialtyCardItem> FeaturedSpecialties { get; set; } = new();
        public List<DoctorCardItem> TopDoctors { get; set; } = new();

        public List<string> AvailableCities { get; set; } = new();
    }

    public class SpecialtyCardItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DoctorCount { get; set; }
        public string? ImageUrl { get; set; }
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
