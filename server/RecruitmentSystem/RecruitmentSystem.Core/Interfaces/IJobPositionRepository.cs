using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IJobPositionRepository
    {
        Task<JobPosition> CreateAsync(JobPosition job);
        Task<JobPosition?> GetByIdAsync(Guid id);
        Task<IEnumerable<JobPosition>> GetActiveAsync();
        Task<IEnumerable<JobPosition>> GetByDepartmentAsync(string department);
        Task<IEnumerable<JobPosition>> GetByStatusAsync(string status);
        Task<List<JobPosition>> SearchPositionsAsync(string searchTerm, string? department = null, string? status = null);
        Task<List<JobPosition>> GetPositionsWithFiltersAsync(
            string? status = null,
            string? department = null,
            string? location = null,
            string? experienceLevel = null,
            List<int>? skillIds = null,
            DateTime? createdFromDate = null,
            DateTime? createdToDate = null,
            DateTime? deadlineFromDate = null,
            DateTime? deadlineToDate = null);
        Task<JobPosition> UpdateAsync(JobPosition job);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task AddSkillsAsync(IEnumerable<JobPositionSkill> skills);
        Task RemoveSkillsAsync(Guid jobPositionId);
    }
}