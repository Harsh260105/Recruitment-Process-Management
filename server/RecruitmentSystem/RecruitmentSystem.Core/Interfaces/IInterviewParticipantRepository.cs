using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IInterviewParticipantRepository
    {
        Task<InterviewParticipant> CreateAsync(InterviewParticipant participant);
        Task<InterviewParticipant?> GetByIdAsync(Guid id);
        Task<IEnumerable<InterviewParticipant>> GetByInterviewAsync(Guid interviewId);
        Task<IEnumerable<InterviewParticipant>> GetByUserAsync(Guid userId);
        Task<InterviewParticipant?> GetByInterviewAndUserAsync(Guid interviewId, Guid userId);
        Task<InterviewParticipant> UpdateAsync(InterviewParticipant participant);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> IsUserParticipantInInterviewAsync(Guid interviewId, Guid userId);
        Task<int> GetParticipantCountForInterviewAsync(Guid interviewId);
    }
}
