using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IInterviewEvaluationRepository
    {
        Task<InterviewEvaluation> CreateAsync(InterviewEvaluation evaluation);
        Task<InterviewEvaluation?> GetByIdAsync(Guid id);
        Task<IEnumerable<InterviewEvaluation>> GetByInterviewAsync(Guid interviewId);
        Task<IEnumerable<InterviewEvaluation>> GetByEvaluatorAsync(Guid evaluatorUserId);
        Task<InterviewEvaluation?> IsUserParticipantInInterviewAsync(Guid interviewId, Guid evaluatorUserId);
        Task<InterviewEvaluation> UpdateAsync(InterviewEvaluation evaluation);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<double> GetAverageScoreForInterviewAsync(Guid interviewId);
        Task<IEnumerable<int?>> GetOverallRatingsByInterviewAsync(Guid interviewId);
        Task<IEnumerable<InterviewEvaluation>> GetEvaluationsForApplicationAsync(Guid applicationId);
        Task<InterviewEvaluation?> GetByInterviewAndEvaluatorAsync(Guid interviewId, Guid evaluatorUserId);
        Task<IEnumerable<EvaluationRecommendation>> GetRecommendationsByInterviewAsync(Guid interviewId);
    }
}