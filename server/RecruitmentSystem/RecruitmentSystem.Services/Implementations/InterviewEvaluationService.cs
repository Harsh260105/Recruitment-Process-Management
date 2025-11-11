using System.Reflection.Metadata.Ecma335;
using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;

namespace RecruitmentSystem.Services.Implementations
{
    /// <summary>
    /// Interview evaluation service implementation
    /// Handles evaluation submission, scoring, and outcome determination
    /// </summary>
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

        /// <summary>
        /// Submits a new interview evaluation
        /// CONTROLLER handles mapping: SubmitEvaluationDto -> InterviewEvaluation entity (via AutoMapper)
        /// SERVICE handles business logic, validation, and authorization
        /// Note: EvaluatorUserId should be set in the evaluation entity by the controller
        /// </summary>
        /// <param name="evaluation">Evaluation entity (mapped from DTO in controller, includes EvaluatorUserId)</param>
        /// <returns>Created evaluation entity</returns>
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

        /// <summary>
        /// Updates an existing interview evaluation
        /// CONTROLLER handles mapping: UpdateEvaluationDto -> InterviewEvaluation entity (via AutoMapper)
        /// SERVICE handles business logic, validation, and audit trail
        /// </summary>
        /// <param name="evaluation">Evaluation entity (mapped from DTO in controller)</param>
        /// <returns>Updated evaluation entity</returns>
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

        /// <summary>
        /// Gets all evaluations for an interview
        /// SERVICE returns Interview entities, CONTROLLER maps to InterviewEvaluationResponseDto
        /// </summary>
        /// <param name="interviewId">Interview ID to get evaluations for</param>
        /// <returns>Collection of evaluation entities</returns>
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

        /// <summary>
        /// Gets evaluation by interview and evaluator
        /// Finds specific evaluation for interviewer with complete evaluation details
        /// </summary>
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

        /// <summary>
        /// Calculates average interview score
        /// Gets all evaluations with ratings, calculates average with proper error handling
        /// </summary>
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

                return Math.Round(average, 2); // Round to 2 decimal places
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average score for interview {InterviewId}", interviewId);
                throw;
            }
        }

        /// <summary>
        /// Determines overall recommendation for interview
        /// Implements business rules for recommendation aggregation with conflict resolution
        /// </summary>
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

        /// <summary>
        /// Sets the final interview outcome
        /// Manual override for HR/managers - allows setting outcome regardless of evaluations
        /// </summary>
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

                    // Get job application to check recruiter assignment
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

        /// <summary>
        /// Gets overall interview outcome for a job application
        /// Aggregates outcomes from all interview rounds using simple business rules
        /// </summary>
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

        /// <summary>
        /// Checks if interview process is complete for a job application
        /// Validates that all interviews are done and have evaluations/outcomes
        /// </summary>
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

                    // Check if interview has final outcome (evaluations are automatically submitted when complete)
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

        /// <summary>
        /// Checks if user can evaluate a specific interview
        /// Verifies user is a participant, interview status, evaluation window, and duplicate check
        /// </summary>
        public async Task<bool> CanEvaluateInterviewAsync(Guid interviewId, Guid evaluatorUserId)
        {
            try
            {
                if (interviewId == Guid.Empty || evaluatorUserId == Guid.Empty)
                    return false;

                // Check if user is a participant in the interview (optimized - no full entity fetch)
                var isParticipant = await _participantRepository.IsUserParticipantInInterviewAsync(interviewId, evaluatorUserId);
                if (!isParticipant)
                    return false;

                // Validate interview is completed (optimized - only status fetch)
                var interviewStatus = await _interviewRepository.GetInterviewStatusAsync(interviewId);
                if (!interviewStatus.HasValue || interviewStatus.Value != InterviewStatus.Completed)
                    return false;

                // For evaluation window check, we need the interview date - get minimal interview data
                var interview = await _interviewRepository.GetByIdAsync(interviewId);
                if (interview == null)
                    return false;

                // Check evaluation window (within 7 days of completion)
                var daysSinceCompletion = (DateTime.UtcNow - interview.ScheduledDateTime).TotalDays;
                if (daysSinceCompletion > 7)
                {
                    _logger.LogWarning("Evaluation window expired for interview {InterviewId}. {Days} days since completion.",
                        interviewId, daysSinceCompletion);
                    return false;
                }

                // Check if user hasn't already submitted evaluation (optimized - direct query)
                var existingEvaluation = await _evaluationRepository.GetByInterviewAndEvaluatorAsync(interviewId, evaluatorUserId);
                if (existingEvaluation != null)
                {
                    // Allow updates within 7 days of original submission
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

        /// <summary>
        /// Checks if interview evaluation is complete
        /// Validates all required participants have submitted evaluations
        /// </summary>
        public async Task<bool> IsInterviewEvaluationCompleteAsync(Guid interviewId)
        {
            try
            {
                if (interviewId == Guid.Empty)
                    return false;

                // Get all participants for the interview
                var participants = await _participantRepository.GetByInterviewAsync(interviewId);
                if (!participants.Any())
                    return false;

                // Get all submitted evaluations
                var evaluations = await _evaluationRepository.GetByInterviewAsync(interviewId);

                var participantIds = participants.Select(p => p.ParticipantUserId).ToHashSet();
                var evaluatorIds = evaluations.Select(e => e.EvaluatorUserId).ToHashSet();

                // Check if all required participants have evaluated
                // For now, all participants are considered mandatory
                // Future enhancement: distinguish between mandatory (PrimaryInterviewer) and optional (Observer)
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

        /// <summary>
        /// Gets interviews requiring evaluation from a specific evaluator
        /// Finds completed interviews where user was participant but hasn't evaluated yet
        /// </summary>
        public async Task<IEnumerable<Interview>> GetInterviewsRequiringEvaluationAsync(Guid evaluatorUserId)
        {
            try
            {
                if (evaluatorUserId == Guid.Empty)
                    throw new ArgumentException("EvaluatorUserId cannot be empty.", nameof(evaluatorUserId));

                // Get interview IDs where user was a participant (optimized - only IDs, not full entities)
                var interviewIds = await _participantRepository.GetInterviewIdsByUserAsync(evaluatorUserId);

                if (!interviewIds.Any())
                {
                    _logger.LogInformation("No interviews found for user {EvaluatorUserId}", evaluatorUserId);
                    return new List<Interview>();
                }

                var pendingInterviews = new List<Interview>();

                foreach (var interviewId in interviewIds)
                {
                    var interview = await _interviewRepository.GetByIdAsync(interviewId);

                    // Filter to completed interviews only
                    if (interview == null || interview.Status != InterviewStatus.Completed)
                        continue;

                    // Check if user already evaluated (optimized - direct query)
                    var existingEvaluation = await _evaluationRepository.GetByInterviewAndEvaluatorAsync(interviewId, evaluatorUserId);
                    if (existingEvaluation != null)
                        continue;

                    // Check evaluation window (not expired)
                    var daysSinceCompletion = (DateTime.UtcNow - interview.ScheduledDateTime).TotalDays;
                    if (daysSinceCompletion > 7)
                        continue;

                    pendingInterviews.Add(interview);
                }

                // Order by priority: deadline urgency, completion date, importance
                var orderedInterviews = pendingInterviews
                    .OrderBy(i => i.ScheduledDateTime) // Earliest completion first (most urgent)
                    .ThenByDescending(i => i.InterviewType == InterviewType.Final ? 1 : 0) // Final interviews have priority
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

        /// <summary>
        /// Validates that the user is a participant in the interview and the interview exists and is completed
        /// </summary>
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

        // TODO: Add private helper methods as needed:
        // - ValidateEvaluationData
        // - CalculateWeightedScore
        // - DetermineEvaluationDeadline
        // - SendEvaluationNotifications
        // - ApplyRecommendationBusinessRules
        // - CheckEvaluatorAuthority
        // - LogEvaluationAction
        // - TriggerWorkflowActions
        // - etc.

        #endregion
    }
}