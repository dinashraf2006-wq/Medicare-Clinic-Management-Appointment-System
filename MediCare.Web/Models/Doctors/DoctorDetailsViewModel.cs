namespace MediCare.Web.Models.Doctors
{
    public class DoctorDetailsViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SpecializationName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = "/images/default-doctor.png";
    }
}
