using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IJobPositionService
    {
        Task<JobPositionResponseDto> CreateJobAsync(CreateJobPositionDto dto, Guid creatorId);
        Task<JobPositionResponseDto?> GetJobByIdAsync(Guid id);

        Task<JobPositionResponseDto> UpdateJobAsync(Guid id, UpdateJobPositionDto dto);
        Task DeleteJobAsync(Guid id);
        Task CloseJobAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);

        Task<PagedResult<JobPositionResponseDto>> GetJobsWithFiltersAsync(
            int pageNumber = 1, int pageSize = 25,
            string? status = null,
            string? department = null,
            string? location = null,
            string? experienceLevel = null,
            List<int>? skillIds = null,
            DateTime? createdFromDate = null,
            DateTime? createdToDate = null,
            DateTime? deadlineFromDate = null,
            DateTime? deadlineToDate = null);
        Task<PagedResult<JobPositionResponseDto>> GetActiveJobsAsync(int pageNumber = 1, int pageSize = 20);
        Task<PagedResult<JobPositionResponseDto>> SearchJobsAsync(
            string searchTerm, int pageNumber = 1, int pageSize = 15, string? department = null, string? status = null);
        Task<PagedResult<JobPositionResponseDto>> GetJobsByDepartmentAsync(string department, int pageNumber = 1, int pageSize = 15);
        Task<PagedResult<JobPositionResponseDto>> GetJobsByStatusAsync(string status, int pageNumber = 1, int pageSize = 15);
    }
}