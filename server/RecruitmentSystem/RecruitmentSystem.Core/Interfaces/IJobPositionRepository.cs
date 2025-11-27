using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Entities.Projections;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IJobPositionRepository
    {
        Task<JobPosition> CreateAsync(JobPosition job);
        Task<JobPosition?> GetByIdAsync(Guid id);

        Task<JobPosition> UpdateAsync(JobPosition job);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> IsJobPositionAvailableForApplicationAsync(Guid jobPositionId);
        Task AddSkillsAsync(IEnumerable<JobPositionSkill> skills);
        Task RemoveSkillsAsync(Guid jobPositionId);

        Task IncrementTotalApplicantsAsync(Guid jobPositionId);

        Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> GetPositionSummariesWithFiltersAsync(
            int pageNumber, int pageSize,
            string? status = null,
            string? department = null,
            string? location = null,
            string? experienceLevel = null,
            List<int>? skillIds = null,
            DateTime? createdFromDate = null,
            DateTime? createdToDate = null,
            DateTime? deadlineFromDate = null,
            DateTime? deadlineToDate = null);

        Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> GetActiveSummariesAsync(int pageNumber, int pageSize);

        Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> SearchPositionSummariesAsync(
            string searchTerm, int pageNumber, int pageSize, string? department = null, string? status = null);

        // Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> GetSummaryByDepartmentAsync(string department, int pageNumber, int pageSize);

        // Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> GetSummaryByStatusAsync(string status, int pageNumber, int pageSize);
    }
}