using MediCare.Services.Admin;

namespace MediCare.Web.Models.Admin;

public class AdminUsersViewModel
{
    public IEnumerable<DoctorStatsDto> DoctorStats { get; set; } = new List<DoctorStatsDto>();
    public IEnumerable<SpecializationStatsDto> SpecializationStats { get; set; } = new List<SpecializationStatsDto>();
}
