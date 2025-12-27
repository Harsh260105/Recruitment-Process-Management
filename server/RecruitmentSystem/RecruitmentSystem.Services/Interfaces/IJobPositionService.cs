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

        Task<PagedResult<TSummary>> GetJobSummariesAsync<TSummary>(
            int pageNumber = 1,
            int pageSize = 25,
            JobPositionQueryDto? query = null)
            where TSummary : JobPositionPublicSummaryDto;

        // Task<PagedResult<TSummary>> GetJobSummariesByDepartmentAsync<TSummary>(string department, int pageNumber = 1, int pageSize = 15)
        //     where TSummary : JobPositionPublicSummaryDto;

        // Task<PagedResult<TSummary>> GetJobSummariesByStatusAsync<TSummary>(string status, int pageNumber = 1, int pageSize = 15)
        //     where TSummary : JobPositionPublicSummaryDto;
    }
}