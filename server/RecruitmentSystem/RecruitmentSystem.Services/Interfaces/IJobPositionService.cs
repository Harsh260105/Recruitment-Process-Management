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

        Task<PagedResult<TSummary>> GetJobSummariesWithFiltersAsync<TSummary>(
            int pageNumber = 1, int pageSize = 25,
            string? status = null,
            string? department = null,
            string? location = null,
            string? experienceLevel = null,
            List<int>? skillIds = null,
            DateTime? createdFromDate = null,
            DateTime? createdToDate = null,
            DateTime? deadlineFromDate = null,
            DateTime? deadlineToDate = null)
            where TSummary : JobPositionPublicSummaryDto;

        Task<PagedResult<TSummary>> GetActiveJobSummariesAsync<TSummary>(int pageNumber = 1, int pageSize = 20)
            where TSummary : JobPositionPublicSummaryDto;

        Task<PagedResult<TSummary>> SearchJobSummariesAsync<TSummary>(
            string searchTerm, int pageNumber = 1, int pageSize = 15,
            string? department = null,
            string? status = null)
            where TSummary : JobPositionPublicSummaryDto;

        // Task<PagedResult<TSummary>> GetJobSummariesByDepartmentAsync<TSummary>(string department, int pageNumber = 1, int pageSize = 15)
        //     where TSummary : JobPositionPublicSummaryDto;

        // Task<PagedResult<TSummary>> GetJobSummariesByStatusAsync<TSummary>(string status, int pageNumber = 1, int pageSize = 15)
        //     where TSummary : JobPositionPublicSummaryDto;
    }
}