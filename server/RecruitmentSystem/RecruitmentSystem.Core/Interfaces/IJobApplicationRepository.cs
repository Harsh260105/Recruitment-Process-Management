using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IJobApplicationRepository
    {
        Task<JobApplication> CreateAsync(JobApplication application);
        Task<JobApplication?> GetByIdAsync(Guid id);
        Task<JobApplication?> GetByIdWithDetailsAsync(Guid id);
        Task<JobApplication?> GetByJobAndCandidateAsync(Guid jobPositionId, Guid candidateProfileId);
        Task<IEnumerable<JobApplication>> GetByJobPositionIdAsync(Guid jobPositionId);
        Task<IEnumerable<JobApplication>> GetByCandidateIdAsync(Guid candidateProfileId);
        Task<IEnumerable<JobApplication>> GetByStatusAsync(ApplicationStatus status);
        Task<IEnumerable<JobApplication>> GetByRecruiterAsync(Guid recruiterId);
        Task<IEnumerable<JobApplication>> GetApplicationsWithFiltersAsync(
            ApplicationStatus? status = null,
            Guid? jobPositionId = null,
            Guid? candidateProfileId = null,
            Guid? assignedRecruiterId = null,
            DateTime? appliedFromDate = null,
            DateTime? appliedToDate = null);
        Task<JobApplication> UpdateAsync(JobApplication application);
        Task<JobApplication> UpdateStatusAsync(Guid applicationId, ApplicationStatus newStatus, Guid changedByUserId, string? comments = null);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> HasCandidateAppliedAsync(Guid jobPositionId, Guid candidateProfileId);
        Task<int> GetApplicationCountByJobAsync(Guid jobPositionId);
        Task<int> GetApplicationCountByStatusAsync(ApplicationStatus status);
        Task<IEnumerable<JobApplication>> GetRecentApplicationsAsync(int count = 10);
    }
}