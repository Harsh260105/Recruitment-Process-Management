using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Services.Interfaces
{
    /// <summary>
    /// Manages interview evaluations, scoring, and outcome determination
    /// </summary>
    public interface IInterviewEvaluationService
    {
        // Evaluation Management
        Task<InterviewEvaluation> SubmitEvaluationAsync(InterviewEvaluation evaluation);
        Task<InterviewEvaluation> UpdateEvaluationAsync(InterviewEvaluation evaluation);
        Task<IEnumerable<InterviewEvaluation>> GetInterviewEvaluationsAsync(Guid interviewId);
        Task<InterviewEvaluation?> GetEvaluationByInterviewAndEvaluatorAsync(Guid interviewId, Guid evaluatorUserId);

        // Scoring and Recommendations
        Task<double> GetAverageInterviewScoreAsync(Guid interviewId);
        Task<EvaluationRecommendation?> GetOverallRecommendationAsync(Guid interviewId);

        // Outcome Processing
        Task<Interview> SetInterviewOutcomeAsync(Guid interviewId, InterviewOutcome outcome, Guid setByUserId);
        Task<InterviewOutcome?> GetOverallInterviewOutcomeForApplicationAsync(Guid jobApplicationId);

        Task<bool> IsInterviewProcessCompleteAsync(Guid jobApplicationId);
    
        // Validation and Business Rules
        Task<bool> CanEvaluateInterviewAsync(Guid interviewId, Guid evaluatorUserId);
        Task<bool> IsInterviewEvaluationCompleteAsync(Guid interviewId);
        Task<IEnumerable<Interview>> GetInterviewsRequiringEvaluationAsync(Guid evaluatorUserId);
    }
}