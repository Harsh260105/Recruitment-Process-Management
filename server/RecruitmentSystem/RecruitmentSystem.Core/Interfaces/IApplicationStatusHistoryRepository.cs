using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IApplicationStatusHistoryRepository
    {
        Task<ApplicationStatusHistory> CreateAsync(ApplicationStatusHistory statusHistory);
        Task<IEnumerable<ApplicationStatusHistory>> GetByApplicationAsync(Guid jobApplicationId);
        Task<IEnumerable<ApplicationStatusHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<ApplicationStatusHistory?> GetLatestStatusChangeAsync(Guid jobApplicationId);
        Task<bool> DeleteAsync(Guid id);
    }
}