using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IInterviewRepository
    {
        Task<Interview> CreateAsync(Interview interview);
        Task<Interview?> GetByIdAsync(Guid id);
        Task<Interview?> GetByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<Interview>> GetByApplicationAsync(Guid jobApplicationId);
        Task<IEnumerable<Interview>> GetByParticipantAsync(Guid participantUserId);
        Task<IEnumerable<Interview>> GetByStatusAsync(InterviewStatus status);
        Task<IEnumerable<Interview>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Interview>> GetScheduledInterviewsAsync(DateTime date);
        Task<IEnumerable<Interview>> GetInterviewsWithFiltersAsync(
            InterviewStatus? status = null,
            InterviewType? interviewType = null,
            InterviewMode? mode = null,
            DateTime? scheduledFromDate = null,
            DateTime? scheduledToDate = null);
        Task<Interview> UpdateAsync(Interview interview);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<IEnumerable<Interview>> GetUpcomingInterviewsForUserAsync(Guid userId, int days = 7);
        Task<IEnumerable<Interview>> GetUpcomingInterviewsForCandidateAsync(Guid candidateUserId, int days = 7);
        Task<IEnumerable<Interview>> GetUpcomingInterviewsForStaffAsync(Guid staffUserId, int days = 7);
        Task<Interview?> GetLatestInterviewForApplicationAsync(Guid applicationId);
        Task<int> GetInterviewCountForApplicationAsync(Guid applicationId);
    }
}