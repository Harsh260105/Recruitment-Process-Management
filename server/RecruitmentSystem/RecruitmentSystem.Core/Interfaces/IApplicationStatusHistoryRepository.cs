using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IApplicationStatusHistoryRepository
    {
        Task<ApplicationStatusHistory> CreateAsync(ApplicationStatusHistory statusHistory);
        Task<IEnumerable<ApplicationStatusHistory>> GetByApplicationAsync(Guid jobApplicationId, int? limit = null);
        Task<IEnumerable<ApplicationStatusHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> DeleteAsync(Guid id);
    }
}