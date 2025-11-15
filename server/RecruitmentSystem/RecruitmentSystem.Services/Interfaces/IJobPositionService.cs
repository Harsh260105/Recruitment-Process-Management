using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IJobPositionService
    {
        Task<JobPositionResponseDto> CreateJobAsync(CreateJobPositionDto dto, Guid creatorId);
        Task<JobPositionResponseDto?> GetJobByIdAsync(Guid id);
        Task<List<JobPositionResponseDto>> GetActiveJobsAsync();
        Task<List<JobPositionResponseDto>> GetJobsByDepartmentAsync(string department);
        Task<List<JobPositionResponseDto>> GetJobsByStatusAsync(string status);
        Task<List<JobPositionResponseDto>> SearchJobsAsync(string searchTerm, string? department = null, string? status = null);
        Task<List<JobPositionResponseDto>> GetJobsWithFiltersAsync(
            string? status = null,
            string? department = null,
            string? location = null,
            string? experienceLevel = null,
            List<int>? skillIds = null,
            DateTime? createdFromDate = null,
            DateTime? createdToDate = null,
            DateTime? deadlineFromDate = null,
            DateTime? deadlineToDate = null);
        Task<JobPositionResponseDto> UpdateJobAsync(Guid id, UpdateJobPositionDto dto);
        Task DeleteJobAsync(Guid id);
        Task CloseJobAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}