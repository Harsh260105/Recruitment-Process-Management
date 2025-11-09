using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;

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

        // Date-based Queries
        Task<IEnumerable<Interview>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, bool includeBasicDetails = false);
        Task<IEnumerable<Interview>> GetScheduledInterviewsAsync(DateTime date);

        // Advanced Filtering
        Task<IEnumerable<Interview>> GetInterviewsWithFiltersAsync(
            InterviewStatus? status = null,
            InterviewType? interviewType = null,
            InterviewMode? mode = null,
            DateTime? scheduledFromDate = null,
            DateTime? scheduledToDate = null,
            bool includeDetails = false);

        // User-specific Queries
        Task<IEnumerable<Interview>> GetUpcomingInterviewsForUserAsync(Guid userId, int days = 7);

        // Application-specific Queries
        Task<Interview?> GetLatestInterviewForApplicationAsync(Guid applicationId);
        Task<int> GetInterviewCountForApplicationAsync(Guid applicationId);
        Task<IEnumerable<Interview>> GetActiveInterviewsByApplicationAsync(Guid jobApplicationId, bool includeEvaluations = false);
    }
}