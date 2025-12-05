using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IJobApplicationManagementService
    {
        // Core CRUD Operations
        Task<JobApplicationDto> CreateApplicationAsync(JobApplicationCreateDto dto, bool consumeOverride = false);
        Task<JobApplicationDetailedDto?> GetApplicationWithDetailsAsync(Guid id);  // Returns detailed DTO with navigation properties
        Task<JobApplicationDto> UpdateApplicationAsync(Guid id, JobApplicationUpdateDto dto, List<string> userRoles);
        Task<bool> DeleteApplicationAsync(Guid id);

        // Collection queries
        Task<PagedResult<JobApplicationSummaryDto>> GetApplicationsByJobForUserAsync(Guid jobPositionId, Guid userId, List<string> userRoles, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<JobApplicationSummaryDto>> GetApplicationsByCandidateAsync(Guid candidateProfileId);
        Task<IEnumerable<JobApplicationSummaryDto>> GetApplicationsByRecruiterAsync(Guid recruiterId);
        Task<PagedResult<JobApplicationSummaryDto>> GetApplicationsByStatusAsync(ApplicationStatus status, int pageNumber = 1, int pageSize = 25);

        // Validation Methods
        Task<JobApplicationEligibilityResult> CanApplyToJobAsync(
            Guid jobPositionId,
            Guid candidateProfileId);

        // Authorization helper - returns entity for access control
        Task<JobApplication?> GetApplicationEntityByIdAsync(Guid id);
    }
}