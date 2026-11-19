
namespace MediCare.Services.Doctors
{
    public class DoctorDto
    {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string SpecializationName { get; set; } = string.Empty;
            public decimal ConsultationFee { get; set; }
            public string? Bio { get; set; }
            public string? ProfileImageUrl { get; set; }

        public string City { get; set; } = string.Empty;

    }
}
