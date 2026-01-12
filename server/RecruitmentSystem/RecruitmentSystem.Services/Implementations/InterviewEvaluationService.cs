using System.Reflection.Metadata.Ecma335;
using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;

namespace RecruitmentSystem.Services.Implementations
{
    public class InterviewEvaluationService : IInterviewEvaluationService
    {
        #region Dependencies

        private readonly IInterviewEvaluationRepository _evaluationRepository;
        private readonly IInterviewRepository _interviewRepository;
        private readonly IInterviewParticipantRepository _participantRepository;
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<InterviewEvaluationService> _logger;

        #endregion

        #region Constructor

        public InterviewEvaluationService(
            IInterviewEvaluationRepository evaluationRepository,
            IInterviewRepository interviewRepository,
            IInterviewParticipantRepository participantRepository,
            IJobApplicationRepository jobApplicationRepository,
            IAuthenticationService authenticationService,
            IEmailService emailService,
            IMapper mapper,
            ILogger<InterviewEvaluationService> logger)
        {
            _evaluationRepository = evaluationRepository;
            _interviewRepository = interviewRepository;
            _participantRepository = participantRepository;
            _jobApplicationRepository = jobApplicationRepository;
            _authenticationService = authenticationService;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion

        #region Evaluation Management

        public async Task<InterviewEvaluation> SubmitEvaluationAsync(InterviewEvaluation evaluation)
        {
            try
            {
                await ValidateEvaluationEligibilityAsync(evaluation.InterviewId, evaluation.EvaluatorUserId);

                bool exists = await _evaluationRepository.ExistsAsync(evaluation.Id);
                if (exists)
                    throw new InvalidOperationException("Evaluation already exists for this evaluator.");

                var savedEvaluation = await _evaluationRepository.CreateAsync(evaluation);

                if (await AllEvaluationsSubmittedAsync(evaluation.InterviewId))
                {
                    _ = SubmitEvaluationToInterview(evaluation.InterviewId);
                }

                return savedEvaluation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting interview evaluation for InterviewId: {InterviewId}, EvaluatorUserId: {EvaluatorUserId}",
                    evaluation.InterviewId, evaluation.EvaluatorUserId);
                throw;
            }
        }

        public async Task<InterviewEvaluation> UpdateEvaluationAsync(InterviewEvaluation evaluation)
        {
            try
            {
                var existingEvaluation = await _evaluationRepository.GetByIdAsync(evaluation.Id);
                if (existingEvaluation == null)
                    throw new ArgumentException("Evaluation not found.");

                if (existingEvaluation.EvaluatorUserId != evaluation.EvaluatorUserId)
                    throw new UnauthorizedAccessException("You can only update your own evaluations.");

                await ValidateEvaluationEligibilityAsync(evaluation.InterviewId, evaluation.EvaluatorUserId);

                if ((DateTime.UtcNow - existingEvaluation.CreatedAt).TotalDays > 7)
                    throw new InvalidOperationException("Evaluation update window has expired.");

                _mapper.Map(evaluation, existingEvaluation);

                var updatedEvaluation = await _evaluationRepository.UpdateAsync(existingEvaluation);

                if (await AllEvaluationsSubmittedAsync(evaluation.InterviewId))
                {
                    _ = SubmitEvaluationToInterview(evaluation.InterviewId);
                }

                return updatedEvaluation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating interview evaluation for InterviewId: {InterviewId}, EvaluatorUserId: {EvaluatorUserId}",
                    evaluation.InterviewId, evaluation.EvaluatorUserId);
                throw;
            }
        }

        public async Task<IEnumerable<InterviewEvaluation>> GetInterviewEvaluationsAsync(Guid interviewId)
        {
            try
            {
                var interview = await _interviewRepository.ExistsAsync(interviewId);
                if (!interview)
                    throw new ArgumentException("Interview does not exist.", nameof(interviewId));

                var evaluations = await _evaluationRepository.GetByInterviewAsync(interviewId);

                var orderedEvaluations = evaluations.OrderBy(e => e.CreatedAt).ToList();

                return orderedEvaluations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving evaluations for interview {InterviewId}", interviewId);
                throw;
            }
        }

        public async Task<InterviewEvaluation?> GetEvaluationByInterviewAndEvaluatorAsync(Guid interviewId, Guid evaluatorUserId)
        {
            try
            {
                var interviewExists = await _interviewRepository.ExistsAsync(interviewId);
                if (!interviewExists)
                    throw new ArgumentException("Interview does not exist.", nameof(interviewId));

                var evaluation = await _evaluationRepository.GetByInterviewAndEvaluatorAsync(interviewId, evaluatorUserId);

                return evaluation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving evaluation for interview {InterviewId} by evaluator {EvaluatorUserId}",
                    interviewId, evaluatorUserId);
                throw;
            }
        }

        #endregion

        #region Scoring and Recommendations

        public async Task<double> GetAverageInterviewScoreAsync(Guid interviewId)
        {
            try
            {
                var interviewExists = await _interviewRepository.ExistsAsync(interviewId);
                if (!interviewExists)
                    throw new ArgumentException("Interview does not exist.", nameof(interviewId));

                var ratings = await _evaluationRepository.GetOverallRatingsByInterviewAsync(interviewId);

                if (!ratings.Any())
                {
                    _logger.LogWarning("No evaluations found for interview {InterviewId}", interviewId);
                    return 0.0;
                }

                var validRatings = ratings.Where(r => r.HasValue).Select(r => r!.Value).ToList();

                if (!validRatings.Any())
                {
                    _logger.LogWarning("No valid ratings found for interview {InterviewId}", interviewId);
                    return 0.0;
                }

                double average = validRatings.Average();

                return Math.Round(average, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average score for interview {InterviewId}", interviewId);
                throw;
            }
        }

        public async Task<EvaluationRecommendation?> GetOverallRecommendationAsync(Guid interviewId)
        {
            try
            {
                var interviewExists = await _interviewRepository.ExistsAsync(interviewId);
                if (!interviewExists)
                    throw new ArgumentException("Interview does not exist.", nameof(interviewId));

                var recommendations = await _evaluationRepository.GetRecommendationsByInterviewAsync(interviewId);

                if (!recommendations.Any())
                {
                    _logger.LogWarning("No evaluations found for interview {InterviewId}", interviewId);
                    return null;
                }

                var recommendationsList = recommendations.ToList();

                var failCount = recommendationsList.Count(r => r == EvaluationRecommendation.Fail);
                var totalCount = recommendationsList.Count;

                if (failCount > totalCount / 2.0)
                {
                    return EvaluationRecommendation.Fail;
                }

                if (recommendationsList.All(r => r == EvaluationRecommendation.Pass))
                {
                    return EvaluationRecommendation.Pass;
                }

                return EvaluationRecommendation.Maybe;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining overall recommendation for interview {InterviewId}", interviewId);
                throw;
            }
        }

        #endregion

        #region Outcome Processing

        public async Task<Interview> SetInterviewOutcomeAsync(Guid interviewId, InterviewOutcome outcome, Guid setByUserId)
        {
            try
            {
                var userRoles = await _authenticationService.GetUserRolesAsync(setByUserId);

                bool canSetOutcome = userRoles.Contains("SuperAdmin") || userRoles.Contains("Admin") ||
                                   userRoles.Contains("HR");

                Interview? interview = null;
                if (userRoles.Contains("Recruiter"))
                {
                    interview = await _interviewRepository.GetByIdAsync(interviewId);
                    if (interview == null)
                        throw new ArgumentException("Interview not found.", nameof(interviewId));

                    var jobApplication = await _jobApplicationRepository.GetByIdAsync(interview.JobApplicationId);
                    if (jobApplication == null)
                        throw new ArgumentException("Job application not found.", nameof(interviewId));

                    if (jobApplication.AssignedRecruiterId == setByUserId)
                    {
                        canSetOutcome = true;
                    }
                }

                if (!canSetOutcome)
                    throw new UnauthorizedAccessException("Insufficient permissions to set interview outcome.");

                if (interview == null)
                {
                    interview = await _interviewRepository.GetByIdAsync(interviewId);
                    if (interview == null)
                        throw new ArgumentException("Interview not found.", nameof(interviewId));
                }

                if (interview.Status != InterviewStatus.Completed)
                    throw new InvalidOperationException("Can only set outcome for completed interviews.");

                interview.Outcome = outcome;

                return await _interviewRepository.UpdateAsync(interview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting interview outcome for InterviewId: {InterviewId}, Outcome: {Outcome}, SetByUserId: {SetByUserId}",
                    interviewId, outcome, setByUserId);
                throw;
            }
        }

        public async Task<InterviewOutcome?> GetOverallInterviewOutcomeForApplicationAsync(Guid jobApplicationId)
        {
            try
            {
                var interviews = await _interviewRepository.GetByApplicationAsync(jobApplicationId);
                if (!interviews.Any())
                {
                    _logger.LogInformation("No interviews found for job application {JobApplicationId}", jobApplicationId);
                    return null;
                }

                var completedInterviews = interviews
                    .Where(i => i.Status == InterviewStatus.Completed && i.Outcome.HasValue)
                    .ToList();

                if (!completedInterviews.Any())
                {
                    _logger.LogInformation("No completed interviews with outcomes for job application {JobApplicationId}", jobApplicationId);
                    return InterviewOutcome.Pending;
                }

                var outcomes = completedInterviews.Select(i => i.Outcome!.Value).ToList();


                if (outcomes.Contains(InterviewOutcome.Fail))
                {
                    _logger.LogInformation("Overall outcome: Fail (contains failed interviews) for application {JobApplicationId}", jobApplicationId);
                    return InterviewOutcome.Fail;
                }

                if (outcomes.All(o => o == InterviewOutcome.Pass))
                {
                    _logger.LogInformation("Overall outcome: Pass (all interviews passed) for application {JobApplicationId}", jobApplicationId);
                    return InterviewOutcome.Pass;
                }

                _logger.LogInformation("Overall outcome: Pending (mixed outcomes or pending interviews) for application {JobApplicationId}", jobApplicationId);
                return InterviewOutcome.Pending;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overall interview outcome for job application {JobApplicationId}", jobApplicationId);
                throw;
            }
        }

        public async Task<bool> IsInterviewProcessCompleteAsync(Guid jobApplicationId)
        {
            try
            {
                var interviews = await _interviewRepository.GetByApplicationAsync(jobApplicationId, includeEvaluations: true);
                if (!interviews.Any())
                {
                    _logger.LogInformation("No interviews found for job application {JobApplicationId} - process incomplete", jobApplicationId);
                    return false;
                }

                foreach (var interview in interviews)
                {
                    if (interview.Status != InterviewStatus.Completed)
                    {
                        _logger.LogInformation("Interview {InterviewId} is not completed - process incomplete for application {JobApplicationId}",
                            interview.Id, jobApplicationId);
                        return false;
                    }

                    if (!interview.Outcome.HasValue || interview.Outcome.Value == InterviewOutcome.Pending)
                    {
                        _logger.LogInformation("Interview {InterviewId} has no final outcome - process incomplete for application {JobApplicationId}",
                            interview.Id, jobApplicationId);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking interview process completion for job application {JobApplicationId}", jobApplicationId);
                return false;
            }
        }

        #endregion

        #region Validation and Business Rules

        public async Task<bool> CanEvaluateInterviewAsync(Guid interviewId, Guid evaluatorUserId)
        {
            try
            {
                if (interviewId == Guid.Empty || evaluatorUserId == Guid.Empty)
                    return false;

                var isParticipant = await _participantRepository.IsUserParticipantInInterviewAsync(interviewId, evaluatorUserId);
                if (!isParticipant)
                    return false;

                var interviewStatus = await _interviewRepository.GetInterviewStatusAsync(interviewId);
                if (!interviewStatus.HasValue || interviewStatus.Value != InterviewStatus.Completed)
                    return false;

                var interview = await _interviewRepository.GetByIdAsync(interviewId);
                if (interview == null)
                    return false;

                var daysSinceCompletion = (DateTime.UtcNow - interview.ScheduledDateTime).TotalDays;
                if (daysSinceCompletion > 7)
                {
                    _logger.LogWarning("Evaluation window expired for interview {InterviewId}. {Days} days since completion.",
                        interviewId, daysSinceCompletion);
                    return false;
                }

                var existingEvaluation = await _evaluationRepository.GetByInterviewAndEvaluatorAsync(interviewId, evaluatorUserId);
                if (existingEvaluation != null)
                {
                    var daysSinceEvaluation = (DateTime.UtcNow - existingEvaluation.CreatedAt).TotalDays;
                    if (daysSinceEvaluation > 7)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking evaluation eligibility for interview {InterviewId} and evaluator {EvaluatorUserId}",
                    interviewId, evaluatorUserId);
                return false;
            }
        }

        public async Task<bool> IsInterviewEvaluationCompleteAsync(Guid interviewId)
        {
            try
            {
                if (interviewId == Guid.Empty)
                    return false;

                var participants = await _participantRepository.GetByInterviewAsync(interviewId);
                if (!participants.Any())
                    return false;

                var evaluations = await _evaluationRepository.GetByInterviewAsync(interviewId);

                var participantIds = participants.Select(p => p.ParticipantUserId).ToHashSet();
                var evaluatorIds = evaluations.Select(e => e.EvaluatorUserId).ToHashSet();

                bool allEvaluated = participantIds.IsSubsetOf(evaluatorIds);

                _logger.LogInformation("Interview {InterviewId} evaluation completion check: {IsComplete}. " +
                    "Participants: {ParticipantCount}, Evaluations: {EvaluationCount}",
                    interviewId, allEvaluated, participants.Count(), evaluations.Count());

                return allEvaluated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking evaluation completion for interview {InterviewId}", interviewId);
                return false;
            }
        }

        public async Task<IEnumerable<Interview>> GetInterviewsRequiringEvaluationAsync(Guid evaluatorUserId)
        {
            try
            {
                if (evaluatorUserId == Guid.Empty)
                    throw new ArgumentException("EvaluatorUserId cannot be empty.", nameof(evaluatorUserId));

                var interviewIds = await _participantRepository.GetInterviewIdsByUserAsync(evaluatorUserId);

                if (!interviewIds.Any())
                {
                    _logger.LogInformation("No interviews found for user {EvaluatorUserId}", evaluatorUserId);
                    return new List<Interview>();
                }

                var pendingInterviews = new List<Interview>();

                foreach (var interviewId in interviewIds)
                {
                    var interview = await _interviewRepository.GetByIdWithFullDetailsAsync(interviewId);

                    if (interview == null || interview.Status != InterviewStatus.Completed)
                        continue;

                    var existingEvaluation = await _evaluationRepository.GetByInterviewAndEvaluatorAsync(interviewId, evaluatorUserId);
                    if (existingEvaluation != null)
                        continue;

                    var daysSinceCompletion = (DateTime.UtcNow - interview.ScheduledDateTime).TotalDays;
                    if (daysSinceCompletion > 7)
                        continue;

                    pendingInterviews.Add(interview);
                }

                var orderedInterviews = pendingInterviews
                    .OrderBy(i => i.ScheduledDateTime)
                    .ThenByDescending(i => i.InterviewType == InterviewType.Final ? 1 : 0)
                    .ToList();

                _logger.LogInformation("Found {Count} interviews requiring evaluation for user {EvaluatorUserId}",
                    orderedInterviews.Count, evaluatorUserId);

                return orderedInterviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending evaluations for user {EvaluatorUserId}", evaluatorUserId);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task ValidateEvaluationEligibilityAsync(Guid interviewId, Guid evaluatorUserId)
        {
            var isParticipant = await _participantRepository.IsUserParticipantInInterviewAsync(interviewId, evaluatorUserId);
            if (!isParticipant)
                throw new UnauthorizedAccessException("User is not a participant in the interview.");

            var interviewStatus = await _interviewRepository.GetInterviewStatusAsync(interviewId);
            if (!interviewStatus.HasValue)
                throw new ArgumentException("Interview does not exist.");
            if (interviewStatus.Value != InterviewStatus.Completed)
                throw new InvalidOperationException("Interview is not completed.");
        }

        private async Task<bool> AllEvaluationsSubmittedAsync(Guid interviewId)
        {
            var participants = await _participantRepository.GetByInterviewAsync(interviewId);
            var evaluations = await _evaluationRepository.GetByInterviewAsync(interviewId);

            var participantIds = participants.Select(p => p.ParticipantUserId).ToHashSet();
            var evaluatorIds = evaluations.Select(e => e.EvaluatorUserId).ToHashSet();

            return participantIds.IsSubsetOf(evaluatorIds);
        }

        private async Task SubmitEvaluationToInterview(Guid interviewId)
        {
            var recommendations = await _evaluationRepository.GetRecommendationsByInterviewAsync(interviewId);
            if (!recommendations.Any())
            {
                _logger.LogWarning("No evaluations found for interview {InterviewId}. Skipping outcome calculation.", interviewId);
                return;
            }

            var recommendationsList = recommendations.ToList();

            InterviewOutcome outcome;

            // 1. Any Fail recommendation = Interview outcome Fail
            var failCount = recommendationsList.Count(r => r == EvaluationRecommendation.Fail);
            var totalCount = recommendationsList.Count;

            if (failCount > totalCount / 2.0)
            {
                outcome = InterviewOutcome.Fail;
                _logger.LogInformation("Overall recommendation: Fail (majority Fail votes: {FailCount}/{TotalCount}) for interview {InterviewId}",
                    failCount, totalCount, interviewId);
            }

            // 2. All Pass = Interview outcome Pass
            else if (recommendationsList.All(r => r == EvaluationRecommendation.Pass))
            {
                outcome = InterviewOutcome.Pass;
                _logger.LogInformation("Setting interview {InterviewId} outcome to Pass (unanimous Pass recommendations)", interviewId);
            }

            // 3. All Maybe or Mixed Pass/Maybe = Interview outcome Pending (needs review)
            else
            {
                outcome = InterviewOutcome.Pending;
                _logger.LogInformation("Setting interview {InterviewId} outcome to Pending (Maybe or mixed recommendations need review)", interviewId);
            }

            var interview = await _interviewRepository.GetByIdAsync(interviewId);
            if (interview == null)
            {
                _logger.LogWarning("Interview {InterviewId} not found. Skipping outcome setting.", interviewId);
                return;
            }

            interview.Outcome = outcome;
            await _interviewRepository.UpdateAsync(interview);
        }

        #endregion
    }
}