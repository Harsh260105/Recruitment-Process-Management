using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IJobApplicationAnalyticsService
    {
        // Search and Filtering
        Task<(List<JobApplication> Items, int TotalCount)> SearchApplicationsAsync(
            ApplicationStatus? status = null,
            Guid? jobPositionId = null,
            Guid? candidateProfileId = null,
            Guid? assignedRecruiterId = null,
            DateTime? appliedFromDate = null,
            DateTime? appliedToDate = null,
            int? minTestScore = null,
            int? maxTestScore = null,
            int pageNumber = 1,
            int pageSize = 20);

        // Statistics and Analytics
        Task<int> GetApplicationCountByJobAsync(Guid jobPositionId);
        Task<int> GetApplicationCountByStatusAsync(ApplicationStatus status);
        Task<(List<JobApplication> Items, int TotalCount)> GetRecentApplicationsAsync(int pageNumber = 1, int pageSize = 10);
        Task<Dictionary<ApplicationStatus, int>> GetApplicationStatusDistributionAsync(Guid? jobPositionId = null);
        Task<(List<JobApplication> Items, int TotalCount)> GetApplicationsRequiringActionAsync(Guid? recruiterId = null, int pageNumber = 1, int pageSize = 20);
    }
}