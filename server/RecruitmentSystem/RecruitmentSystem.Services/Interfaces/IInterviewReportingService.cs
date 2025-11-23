using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;

namespace RecruitmentSystem.Services.Interfaces
{
    /// <summary>
    /// Provides reporting, analytics, and search capabilities for interviews
    /// </summary>
    public interface IInterviewReportingService
    {
        // Analytics and Statistics (with caching consideration)
        Task<Dictionary<InterviewStatus, int>> GetInterviewStatusDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<Dictionary<InterviewType, int>> GetInterviewTypeDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<InterviewAnalyticsDto> GetInterviewAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

        // Search and Filtering (with pagination)
        Task<PagedResult<InterviewSummaryDto>> SearchInterviewsAsync(
            InterviewSearchDto searchDto);

        Task<PagedResult<Interview>> GetUpcomingInterviewsForUserAsync(Guid userId, int days = 7, int pageNumber = 1, int pageSize = 20);
        Task<PagedResult<Interview>> GetTodayInterviewsAsync(Guid? participantUserId = null, int pageNumber = 1, int pageSize = 20);
        Task<PagedResult<Interview>> GetInterviewsNeedingActionAsync(Guid? userId = null, int pageNumber = 1, int pageSize = 20);

        // Application-specific Queries
        Task<PagedResult<Interview>> GetInterviewsByApplicationAsync(Guid jobApplicationId, int pageNumber = 1, int pageSize = 20);
        Task<PagedResult<Interview>> GetInterviewsByParticipantAsync(Guid participantUserId, int pageNumber = 1, int pageSize = 20);
    }
}