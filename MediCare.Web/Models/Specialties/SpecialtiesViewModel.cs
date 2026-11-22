using MediCare.Web.Models.Home;

namespace MediCare.Web.Models.Specialties
{
    public class SpecialtiesViewModel
    {
        public string? SearchQuery { get; set; }
        public List<SpecialtyCardItem> Specialties { get; set; } = new();
    }

    public class SpecialtyCardItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DoctorCount { get; set; }
        public string? ImageUrl { get; set; }
    }
}
