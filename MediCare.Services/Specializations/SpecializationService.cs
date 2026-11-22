using MediCare.Data.Entities;
using MediCare.Data.Repositories;

namespace MediCare.Services.Specializations
{
    public class SpecializationService : ISpecializationService
    {
        private readonly IUnitOfWork _unitOfWork;
        public SpecializationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IReadOnlyList<Specialization>> GetAllSpecializationsAsync(string? search = null)
        {
            return await _unitOfWork.Specialization.GetAllWithDoctorsAsync(search);
        }
    }

    public interface ISpecializationService
    {
        Task<IReadOnlyList<Specialization>> GetAllSpecializationsAsync(string? search = null);
    }
}
