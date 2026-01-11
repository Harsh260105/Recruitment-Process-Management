using System.Collections.Generic;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.DTOs;
using RecruitmentSystem.Core.Entities.Projections;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IInterviewRepository
    {
        // Basic CRUD Operations
        Task<Interview> CreateAsync(Interview interview);
        Task<Interview?> GetByIdAsync(Guid id);
        Task<Interview> UpdateAsync(Interview interview);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<Interview?> GetByIdWithFullDetailsAsync(Guid id);
        Task<IEnumerable<Interview>> GetByApplicationAsync(Guid jobApplicationId, bool includeEvaluations = false);
        Task<IEnumerable<Interview>> GetByParticipantAsync(Guid participantUserId, bool includeCandidateInfo = false);
        Task<IEnumerable<Interview>> GetByStatusAsync(InterviewStatus status, bool includeDetails = false);
        Task<IEnumerable<Interview>> GetCompletedInterviewsInDateRangeAsync(DateTime start, DateTime end, bool includeDetails = false);

        // Date-based Queries
        Task<IEnumerable<Interview>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, bool includeBasicDetails = false);
        Task<IEnumerable<Interview>> GetScheduledInterviewsWithDetailsAsync(
            DateTime startDate,
            DateTime endDate,
            IEnumerable<Guid> participantUserIds,
            Guid? excludeJobApplicationId = null);

        Task<Dictionary<Guid, List<Interview>>> GetScheduledInterviewsByParticipantUserIdsAsync(
            List<Guid> participantUserIds,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeJobApplicationId = null);

        Task<(List<InterviewSummaryProjection> Items, int TotalCount)> SearchInterviewSummariesAsync(
            Guid? userId,
            InterviewStatus? status = null,
            InterviewType? interviewType = null,
            InterviewMode? mode = null,
            DateTime? scheduledFromDate = null,
            DateTime? scheduledToDate = null,
            Guid? participantUserId = null,
            Guid? jobApplicationId = null,
            int pageNumber = 1,
            int pageSize = 20);

        // User-specific Queries
        Task<(List<InterviewSummaryProjection> Items, int TotalCount)> GetUpcomingInterviewSummariesForUserAsync(Guid userId, int days, int pageNumber, int pageSize);

        Task<(List<InterviewSummaryProjection> Items, int TotalCount)> GetTodayInterviewSummariesAsync(DateTime date, Guid? participantUserId, int pageNumber, int pageSize);

        // Application-specific Queries
        Task<Interview?> GetLatestInterviewForApplicationAsync(Guid applicationId);
        Task<int> GetInterviewCountForApplicationAsync(Guid applicationId);
        Task<IEnumerable<Interview>> GetActiveInterviewsByApplicationAsync(Guid jobApplicationId, bool includeEvaluations = false);
        Task<(List<InterviewSummaryProjection> Items, int TotalCount)> GetInterviewSummariesByApplicationAsync(Guid jobApplicationId, int pageNumber, int pageSize);
        Task<(List<InterviewSummaryProjection> Items, int TotalCount)> GetInterviewSummariesByParticipantAsync(Guid participantUserId, int pageNumber, int pageSize);
        Task<InterviewStatus?> GetInterviewStatusAsync(Guid interviewId);
        Task<InterviewDetailProjection?> GetInterviewDetailProjectionAsync(Guid interviewId);
        Task<(List<InterviewNeedingActionProjection> Items, int TotalCount)> GetInterviewsNeedingActionProjectionAsync(Guid? userId, bool isPrivilegedStaff, bool isRecruiter, int pageNumber = 1, int pageSize = 20);
    }
}