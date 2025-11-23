using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;

namespace RecruitmentSystem.Services.Implementations
{
    public class JobApplicationAnalyticsService : IJobApplicationAnalyticsService
    {
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly ILogger<JobApplicationAnalyticsService> _logger;

        public JobApplicationAnalyticsService(
            IJobApplicationRepository jobApplicationRepository,
            ILogger<JobApplicationAnalyticsService> logger)
        {
            _jobApplicationRepository = jobApplicationRepository;
            _logger = logger;
        }

        #region Search and Filtering

        public async Task<(List<JobApplication> Items, int TotalCount)> SearchApplicationsAsync(
            ApplicationStatus? status = null,
            Guid? jobPositionId = null,
            Guid? candidateProfileId = null,
            Guid? assignedRecruiterId = null,
            DateTime? appliedFromDate = null,
            DateTime? appliedToDate = null,
            int? minTestScore = null,
            int? maxTestScore = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var applications = await _jobApplicationRepository.GetApplicationsWithFiltersAsync(
                    status, jobPositionId, candidateProfileId, assignedRecruiterId,
                    appliedFromDate, appliedToDate);

                // Apply test score filtering if specified
                if (minTestScore.HasValue || maxTestScore.HasValue)
                {
                    applications = applications.Where(app =>
                    {
                        if (!app.TestScore.HasValue) return false;

                        if (minTestScore.HasValue && app.TestScore.Value < minTestScore.Value) return false;
                        if (maxTestScore.HasValue && app.TestScore.Value > maxTestScore.Value) return false;

                        return true;
                    }).ToList();
                }

                var totalCount = applications.Count();
                var paginatedItems = applications
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (paginatedItems, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching applications with provided filters");
                throw;
            }
        }

        #endregion

        #region Statistics and Analytics

        public async Task<int> GetApplicationCountByJobAsync(Guid jobPositionId)
        {
            try
            {
                var count = await _jobApplicationRepository.GetApplicationCountByJobAsync(jobPositionId);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application count for job {JobPositionId}", jobPositionId);
                throw;
            }
        }

        public async Task<int> GetApplicationCountByStatusAsync(ApplicationStatus status)
        {
            try
            {
                var count = await _jobApplicationRepository.GetApplicationCountByStatusAsync(status);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application count for status {Status}", status);
                throw;
            }
        }

        public async Task<(List<JobApplication> Items, int TotalCount)> GetRecentApplicationsAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                // Get more than needed to calculate total, or use a count query first
                var allRecentApplications = await _jobApplicationRepository.GetRecentApplicationsAsync(pageSize * 10); // Get enough data
                var totalCount = allRecentApplications.Count();

                var paginatedItems = allRecentApplications
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (paginatedItems, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent applications");
                throw;
            }
        }

        public async Task<Dictionary<ApplicationStatus, int>> GetApplicationStatusDistributionAsync(Guid? jobPositionId = null)
        {
            try
            {
                var distribution = await _jobApplicationRepository.GetApplicationStatusDistributionAsync(jobPositionId);

                return distribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status distribution for job {JobPositionId}", jobPositionId);
                throw;
            }
        }

        public async Task<(List<JobApplication> Items, int TotalCount)> GetApplicationsRequiringActionAsync(Guid? recruiterId = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var applications = await _jobApplicationRepository.GetApplicationsRequiringActionAsync(recruiterId);
                var applicationsList = applications.ToList();
                var totalCount = applicationsList.Count;

                var paginatedItems = applicationsList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (paginatedItems, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications requiring action for recruiter {RecruiterId}", recruiterId);
                throw;
            }
        }

        #endregion
    }
}