using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations
{
    /// <summary>
    /// Core interview management service implementation
    /// Handles basic CRUD operations and core business workflows
    /// </summary>
    public class InterviewService : IInterviewService
    {
        #region Dependencies

        private readonly IInterviewRepository _interviewRepository;
        private readonly IInterviewParticipantRepository _participantRepository;
        private readonly IInterviewEvaluationRepository _evaluationRepository;
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<InterviewService> _logger;

        #endregion

        #region Constructor

        public InterviewService(
            IInterviewRepository interviewRepository,
            IInterviewParticipantRepository participantRepository,
            IInterviewEvaluationRepository evaluationRepository,
            IJobApplicationRepository jobApplicationRepository,
            IAuthenticationService authenticationService,
            IMapper mapper,
            ILogger<InterviewService> logger)
        {
            _interviewRepository = interviewRepository;
            _participantRepository = participantRepository;
            _evaluationRepository = evaluationRepository;
            _jobApplicationRepository = jobApplicationRepository;
            _authenticationService = authenticationService;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion

        #region Core CRUD Operations

        /// <summary>
        /// Creates interview from DTO - Primary method for service operations
        /// SERVICE handles mapping: CreateInterviewDto -> Interview entity (via AutoMapper)
        /// Note: This creates the interview entity, but actual scheduling logic with 
        /// advanced validation should be done through InterviewSchedulingService
        /// </summary>
        public async Task<Interview> CreateInterviewAsync(CreateInterviewDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            try
            {
                var jobApplication = await _jobApplicationRepository.GetByIdAsync(dto.JobApplicationId);
                if (jobApplication == null)
                    throw new InvalidOperationException($"Job application {dto.JobApplicationId} not found");

                var interview = _mapper.Map<Interview>(dto);

                await ValidateInterviewBusinessRulesAsync(interview, jobApplication);

                var createdInterview = await _interviewRepository.CreateAsync(interview);

                return createdInterview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create interview for Job Application {JobApplicationId}", dto?.JobApplicationId);
                throw;
            }
        }



        /// <summary>
        /// Gets interview by ID with optional details
        /// SERVICE handles mapping: Interview entity -> InterviewResponseDto (via AutoMapper)
        /// </summary>
        /// <param name="id">Interview ID</param>
        /// <param name="includeDetails">Whether to include participants and evaluations</param>
        /// <returns>Interview entity for service layer operations</returns>
        public async Task<Interview?> GetInterviewByIdAsync(Guid id, bool includeDetails = false)
        {
            try
            {
                if (id == Guid.Empty)
                    throw new ArgumentException("Interview ID cannot be empty", nameof(id));

                if (!includeDetails)
                {
                    // Lightweight fetch
                    var interview = await _interviewRepository.GetByIdAsync(id);
                    if (interview == null)
                    {
                        _logger.LogWarning("Interview {InterviewId} not found", id);
                        return null;
                    }

                    return interview;
                }

                return await _interviewRepository.GetByIdWithFullDetailsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve interview {InterviewId}", id);
                throw;
            }
        }



        /// <summary>
        /// Updates an existing interview using DTO
        /// SERVICE handles mapping: UpdateInterviewDto -> Interview entity updates (via AutoMapper)
        /// </summary>
        public async Task<Interview> UpdateInterviewAsync(Guid interviewId, UpdateInterviewDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            try
            {
                var existingInterview = await _interviewRepository.GetByIdAsync(interviewId);
                if (existingInterview == null)
                    throw new InvalidOperationException($"Interview {interviewId} not found");

                await ValidateInterviewCanBeModified(existingInterview);

                _mapper.Map(dto, existingInterview);

                var updatedInterview = await _interviewRepository.UpdateAsync(existingInterview);

                return updatedInterview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update interview {InterviewId}", interviewId);
                throw;
            }
        }



        /// <summary>
        /// Deletes an interview (soft delete)
        /// Validates business rules and handles related data cleanup
        /// </summary>
        public async Task<bool> DeleteInterviewAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    throw new ArgumentException("Interview ID cannot be empty", nameof(id));

                var interview = await _interviewRepository.GetByIdAsync(id);
                if (interview == null)
                {
                    _logger.LogWarning("Cannot delete interview {InterviewId} - not found", id);
                    return false;
                }

                // Validate business rules for deletion
                if (!await ValidateInterviewCanBeDeletedInternalAsync(id))
                {
                    _logger.LogWarning("Cannot delete interview {InterviewId} - business rules prevent deletion", id);
                    return false;
                }

                // Soft delete (mark as inactive)
                interview.IsActive = false;

                await _interviewRepository.UpdateAsync(interview);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete interview {InterviewId}", id);
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Validates business rules for interview creation/update
        /// Simplified enterprise-grade validation - round number validation handled by InterviewSchedulingService
        /// </summary>
        private async Task ValidateInterviewBusinessRulesAsync(Interview interview, JobApplication jobApplication)
        {
            ArgumentNullException.ThrowIfNull(interview);
            ArgumentNullException.ThrowIfNull(jobApplication);

            ValidateInterviewDataIntegrity(interview);

            ValidateJobApplicationStatus(jobApplication);

            await ValidatePendingInterviewRestrictionAsync(interview.JobApplicationId);

            await ValidateSchedulingConflictsAsync(interview, jobApplication.Id);
        }

        /// <summary>
        /// Validates basic interview data integrity
        /// Simplified - basic validations only, scheduling-specific rules handled by InterviewSchedulingService
        /// </summary>
        private static void ValidateInterviewDataIntegrity(Interview interview)
        {
            // Basic data validation only
            if (interview.ScheduledDateTime <= DateTime.UtcNow)
            {
                throw new ArgumentException("Interview cannot be scheduled in the past");
            }

            if (interview.DurationMinutes <= 0 || interview.DurationMinutes > 480) // Max 8 hours
            {
                throw new ArgumentException("Interview duration must be between 1 minute and 8 hours");
            }

            if (string.IsNullOrWhiteSpace(interview.Title))
            {
                throw new ArgumentException("Interview title is required");
            }

            if (interview.RoundNumber <= 0)
            {
                throw new ArgumentException("Round number must be positive");
            }

            // Note: Advanced time slot validation (business hours, weekends, advance notice) 
            // is handled by InterviewSchedulingService when interviews are actually scheduled
            // This keeps the core service focused on basic entity validation
        }

        /// <summary>
        /// Validates job application status allows interview scheduling
        /// </summary>
        private static void ValidateJobApplicationStatus(JobApplication jobApplication)
        {
            var validStatusesForScheduling = new[] {
                ApplicationStatus.Shortlisted,
                ApplicationStatus.Interview,
                ApplicationStatus.UnderReview,
                ApplicationStatus.TestCompleted
            };

            if (!validStatusesForScheduling.Contains(jobApplication.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot schedule interview for application in status {jobApplication.Status}. " +
                    $"Valid statuses: {string.Join(", ", validStatusesForScheduling)}");
            }

            // Check if application is active
            if (!jobApplication.IsActive)
            {
                throw new InvalidOperationException("Cannot schedule interview for inactive application");
            }
        }

        /// <summary>
        /// Validates that no pending scheduled interviews exist for the application
        /// Business rule: One scheduled interview at a time per application
        /// </summary>
        private async Task ValidatePendingInterviewRestrictionAsync(Guid jobApplicationId)
        {
            var activeInterviews = await _interviewRepository.GetActiveInterviewsByApplicationAsync(jobApplicationId);

            // If there are scheduled interviews that haven't been completed, don't allow new scheduling
            var pendingInterview = activeInterviews.FirstOrDefault(i =>
                i.Status == InterviewStatus.Scheduled &&
                i.ScheduledDateTime > DateTime.UtcNow);

            if (pendingInterview != null)
            {
                throw new InvalidOperationException(
                    $"Cannot schedule new interview - pending interview {pendingInterview.Id} exists for this application");
            }
        }

        /// <summary>
        /// Validates scheduling conflicts
        /// Simplified - only check direct time overlaps
        /// </summary>
        private async Task ValidateSchedulingConflictsAsync(Interview interview, Guid jobApplicationId)
        {
            var activeInterviews = await _interviewRepository.GetActiveInterviewsByApplicationAsync(jobApplicationId);
            var scheduledInterviews = activeInterviews
                .Where(i => i.Status == InterviewStatus.Scheduled)
                .ToList();

            // Check for direct time overlap
            var interviewEnd = interview.ScheduledDateTime.AddMinutes(interview.DurationMinutes);

            const int bufferMinutes = 30;

            var conflictingInterview = scheduledInterviews.FirstOrDefault(i =>
            {
                var existingEnd = i.ScheduledDateTime.AddMinutes(i.DurationMinutes);
                var existingStartWithBuffer = i.ScheduledDateTime.AddMinutes(-bufferMinutes);
                var existingEndWithBuffer = existingEnd.AddMinutes(bufferMinutes);

                return interview.ScheduledDateTime < existingEndWithBuffer && interviewEnd > existingStartWithBuffer;
            });

            if (conflictingInterview != null)
            {
                throw new InvalidOperationException(
                    $"Interview time overlaps with existing interview on {conflictingInterview.ScheduledDateTime:yyyy-MM-dd HH:mm}");
            }
        }



        /// <summary>
        /// Validates that interview can be modified in its current state
        /// Minimal approach: Only prevent modifications to completed interviews with evaluations
        /// </summary>
        private async Task ValidateInterviewCanBeModified(Interview interview)
        {
            if (interview.Status == InterviewStatus.Completed)
            {
                var evaluations = await _evaluationRepository.GetByInterviewAsync(interview.Id);
                if (evaluations.Any())
                {
                    throw new InvalidOperationException(
                        "Cannot modify completed interview that has evaluations");
                }
            }
        }



        /// <summary>
        /// Validates that interview can be deleted - internal implementation
        /// </summary>
        private async Task<bool> ValidateInterviewCanBeDeletedInternalAsync(Guid interviewId)
        {
            var interview = await _interviewRepository.GetByIdAsync(interviewId);
            if (interview == null) return false;

            // Cannot delete completed interviews with evaluations
            if (interview.Status == InterviewStatus.Completed)
            {
                var evaluations = await _evaluationRepository.GetByInterviewAsync(interviewId);
                if (evaluations.Any())
                {
                    throw new InvalidOperationException(
                        "Cannot delete completed interview that has evaluations");
                }
            }

            // Cannot delete if it's the only interview and job application status is Interview
            var jobApplication = await _jobApplicationRepository.GetByIdAsync(interview.JobApplicationId);
            if (jobApplication?.Status == ApplicationStatus.Interview)
            {
                var otherActiveInterviews = await _interviewRepository.GetActiveInterviewsByApplicationAsync(interview.JobApplicationId);
                if (otherActiveInterviews.Count(i => i.Id != interviewId) == 0)
                {
                    throw new InvalidOperationException(
                        "Cannot delete the only interview when application status is Interview");
                }
            }

            return true;
        }

        #endregion
    }
}