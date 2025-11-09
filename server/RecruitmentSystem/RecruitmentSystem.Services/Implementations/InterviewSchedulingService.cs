using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations
{
    /// <summary>
    /// Interview scheduling service implementation
    /// Handles scheduling, rescheduling, cancellation, and participant management
    /// </summary>
    public class InterviewSchedulingService : IInterviewSchedulingService
    {
        #region Dependencies

        private readonly IInterviewService _interviewService; // Core interview operations
        private readonly IInterviewRepository _interviewRepository;
        private readonly IInterviewParticipantRepository _participantRepository;
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService; // For notifications
        private readonly IMeetingService _meetingService; // For video conferencing
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ILogger<InterviewSchedulingService> _logger;

        #endregion

        #region Constructor

        public InterviewSchedulingService(
            IInterviewService interviewService,
            IInterviewRepository interviewRepository,
            IInterviewParticipantRepository participantRepository,
            IJobApplicationRepository jobApplicationRepository,
            UserManager<User> userManager,
            IEmailService emailService,
            IMeetingService meetingService,
            IConfiguration configuration,
            IMapper mapper,
            ILogger<InterviewSchedulingService> logger)
        {
            _interviewService = interviewService;
            _interviewRepository = interviewRepository;
            _participantRepository = participantRepository;
            _jobApplicationRepository = jobApplicationRepository;
            _userManager = userManager;
            _emailService = emailService;
            _meetingService = meetingService;
            _configuration = configuration;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion

        #region Core Scheduling Operations

        /// <summary>
        /// Schedules a new interview - MAIN USER-FACING METHOD
        /// This is the primary method for creating AND scheduling interviews
        /// ENTERPRISE FLOW: ScheduleInterviewDto -> Interview creation -> Participant management -> Notifications
        /// </summary>
        /// <param name="dto">Interview scheduling data from controller</param>
        /// <param name="scheduledByUserId">User scheduling the interview (from controller context)</param>
        /// <returns>Created interview entity with participants</returns>
        public async Task<Interview> ScheduleInterviewAsync(ScheduleInterviewDto dto, Guid scheduledByUserId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dto);

                await ValidateSchedulingRequestAsync(dto);

                ValidateTimeSlot(dto.ScheduledDateTime, dto.DurationMinutes);

                foreach (var participantId in dto.ParticipantUserIds)
                {
                    if (await HasConflictingInterviewsAsync(participantId, dto.ScheduledDateTime, dto.DurationMinutes))
                    {
                        var participantUser = await _userManager.FindByIdAsync(participantId.ToString());
                        throw new InvalidOperationException(
                            $"Participant {participantUser?.Email ?? participantId.ToString()} has conflicting interviews at the scheduled time");
                    }
                }

                var roundNumber = await DetermineNextRoundNumberAsync(dto.JobApplicationId);

                var createDto = _mapper.Map<CreateInterviewDto>(dto);
                createDto.RoundNumber = roundNumber;
                createDto.ScheduledByUserId = scheduledByUserId;

                var interview = await _interviewService.CreateInterviewAsync(createDto);

                await AddParticipantsToInterviewAsync(interview.Id, dto.ParticipantUserIds);

                if (interview.Mode == InterviewMode.Online)
                {
                    var participants = await _participantRepository.GetByInterviewAsync(interview.Id);
                    var participantEmails = participants
                        .Where(p => !string.IsNullOrEmpty(p.ParticipantUser.Email))
                        .Select(p => p.ParticipantUser.Email!)
                        .ToList();

                    var jobApplication = await _jobApplicationRepository.GetByIdAsync(interview.JobApplicationId);
                    if (!string.IsNullOrEmpty(jobApplication?.CandidateProfile?.User?.Email))
                    {
                        participantEmails.Add(jobApplication.CandidateProfile.User.Email);
                    }

                    var meetingDetails = await GenerateMeetingDetailsAsync(interview, participantEmails);

                    if (!string.IsNullOrEmpty(meetingDetails))
                    {
                        interview.MeetingDetails = meetingDetails;
                        await _interviewRepository.UpdateAsync(interview);
                    }
                }

                if (roundNumber == 1)
                {
                    await UpdateApplicationStatusAsync(
                        dto.JobApplicationId,
                        ApplicationStatus.Interview,
                        scheduledByUserId,
                        $"First interview scheduled: {interview.Title} on {interview.ScheduledDateTime:yyyy-MM-dd HH:mm}",
                        "Updated application status to Interview for first interview");
                }

                await SendSchedulingNotificationsAsync(interview, dto.ParticipantUserIds);

                _logger.LogInformation("Interview scheduled successfully: {InterviewId} for application {ApplicationId}",
                    interview.Id, dto.JobApplicationId);

                return interview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule interview for application {ApplicationId}", dto?.JobApplicationId);
                throw;
            }
        }



        /// <summary>
        /// Reschedules an existing interview
        /// Enterprise-grade rescheduling with comprehensive validation and notifications
        /// </summary>
        /// <param name="interviewId">Interview to reschedule</param>
        /// <param name="dto">Rescheduling data from controller</param>
        /// <param name="rescheduledByUserId">User performing the reschedule (from controller context)</param>
        /// <returns>Updated interview entity</returns>
        public async Task<Interview> RescheduleInterviewAsync(Guid interviewId, RescheduleInterviewDto dto, Guid rescheduledByUserId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dto);

                var existingInterview = await GetAndValidateInterviewAsync(interviewId, "rescheduling");

                await ValidateInterviewCanBeRescheduledAsync(existingInterview, rescheduledByUserId);

                ValidateTimeSlot(dto.NewDateTime, existingInterview.DurationMinutes);

                var participants = await _participantRepository.GetByInterviewAsync(interviewId);
                foreach (var participant in participants)
                {
                    if (await HasConflictingInterviewsAsync(participant.ParticipantUserId, dto.NewDateTime, existingInterview.DurationMinutes))
                    {
                        var participantUser = await _userManager.FindByIdAsync(participant.ParticipantUserId.ToString());
                        throw new InvalidOperationException(
                            $"Rescheduled time conflicts with existing interviews for participant {participantUser?.Email ?? participant.ParticipantUserId.ToString()}");
                    }
                }

                var originalDateTime = existingInterview.ScheduledDateTime;

                var updateDto = new UpdateInterviewDto
                {
                    ScheduledDateTime = dto.NewDateTime
                };

                var updatedInterview = await _interviewService.UpdateInterviewAsync(interviewId, updateDto);

                var reschedulingNote = $"Rescheduled from {originalDateTime:yyyy-MM-dd HH:mm} to {dto.NewDateTime:yyyy-MM-dd HH:mm}. Reason: {dto.Reason ?? "Not specified"}";
                var summaryNotes = string.IsNullOrEmpty(updatedInterview.SummaryNotes)
                    ? reschedulingNote
                    : $"{updatedInterview.SummaryNotes}\n{reschedulingNote}";

                updatedInterview.SummaryNotes = summaryNotes;
                updatedInterview = await _interviewRepository.UpdateAsync(updatedInterview);

                await SendReschedulingNotificationsAsync(updatedInterview, originalDateTime, participants);

                return updatedInterview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reschedule interview {InterviewId}", interviewId);
                throw;
            }
        }

        /// <summary>
        /// Cancels an interview
        /// Enterprise-grade cancellation with comprehensive cleanup and notifications
        /// </summary>
        public async Task<Interview> CancelInterviewAsync(Guid interviewId, CancelInterviewDto dto, Guid cancelledByUserId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dto);

                var existingInterview = await GetAndValidateInterviewAsync(interviewId, "cancellation");

                await ValidateInterviewCanBeCancelledAsync(existingInterview, cancelledByUserId);

                var participants = await _participantRepository.GetByInterviewAsync(interviewId);

                existingInterview.Status = InterviewStatus.Cancelled;
                var cancellationNote = $"Interview cancelled on {DateTime.UtcNow:yyyy-MM-dd HH:mm}. Reason: {dto.Reason ?? "Not specified"}";
                existingInterview.SummaryNotes = string.IsNullOrEmpty(existingInterview.SummaryNotes)
                    ? cancellationNote
                    : $"{existingInterview.SummaryNotes}\n{cancellationNote}";

                var updatedInterview = await _interviewRepository.UpdateAsync(existingInterview);

                await SendCancellationNotificationsAsync(updatedInterview, participants, dto.Reason);

                return updatedInterview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel interview {InterviewId}", interviewId);
                throw;
            }
        }

        #endregion

        #region Status Management

        /// <summary>
        /// Marks interview as completed
        /// Enterprise-grade completion with evaluation reminders and workflow integration
        /// </summary>
        public async Task<Interview> MarkInterviewAsCompletedAsync(Guid interviewId, MarkInterviewCompletedDto dto, Guid completedByUserId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dto);

                var existingInterview = await GetAndValidateInterviewAsync(interviewId, "completion");

                if (existingInterview.Status != InterviewStatus.Scheduled)
                    throw new InvalidOperationException($"Cannot complete interview in {existingInterview.Status} status - must be Scheduled");

                if (existingInterview.ScheduledDateTime.AddMinutes(10) > DateTime.UtcNow)
                    throw new InvalidOperationException("Cannot complete interview before scheduled time plus 10 minutes");

                var participants = await _participantRepository.GetByInterviewAsync(interviewId);

                existingInterview.Status = InterviewStatus.Completed;
                var completionNote = $"Interview completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm} by {completedByUserId}";

                if (!string.IsNullOrEmpty(dto.SummaryNotes))
                {
                    completionNote += $"\nSummary: {dto.SummaryNotes}";
                }

                existingInterview.SummaryNotes = string.IsNullOrEmpty(existingInterview.SummaryNotes)
                    ? completionNote
                    : $"{existingInterview.SummaryNotes}\n{completionNote}";

                var updatedInterview = await _interviewRepository.UpdateAsync(existingInterview);

                await UpdateApplicationStatusForCompletionAsync(updatedInterview, completedByUserId);

                await SendEvaluationRemindersAsync(updatedInterview, participants);

                return updatedInterview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark interview as completed: {InterviewId}", interviewId);
                throw;
            }
        }

        /// <summary>
        /// Marks interview as no-show
        /// Enterprise-grade no-show handling with rescheduling options
        /// </summary>
        public async Task<Interview> MarkNoShowAsync(Guid interviewId, MarkInterviewNoShowDto dto, Guid markedByUserId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dto);

                var existingInterview = await GetAndValidateInterviewAsync(interviewId, "no-show marking");

                await ValidateInterviewCanBeMarkedNoShowAsync(existingInterview, markedByUserId);

                // 3. Get participants for notifications
                var participants = await _participantRepository.GetByInterviewAsync(interviewId);

                // 4. Update interview status and add no-show notes
                existingInterview.Status = InterviewStatus.NoShow;
                var noShowNote = $"Marked as no-show on {DateTime.UtcNow:yyyy-MM-dd HH:mm} by user {markedByUserId}";

                if (!string.IsNullOrEmpty(dto.Notes))
                {
                    noShowNote += $"\nNotes: {dto.Notes}";
                }

                existingInterview.SummaryNotes = string.IsNullOrEmpty(existingInterview.SummaryNotes)
                    ? noShowNote
                    : $"{existingInterview.SummaryNotes}\n{noShowNote}";

                var updatedInterview = await _interviewRepository.UpdateAsync(existingInterview);

                // 6. Send no-show notifications
                await SendNoShowNotificationsAsync(updatedInterview, participants);

                return updatedInterview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark interview as no-show: {InterviewId}", interviewId);
                throw;
            }
        }

        #endregion

        #region Participant Management

        /// <summary>
        /// Gets all participants for an interview
        /// Retrieves participants with user details and proper ordering
        /// - Retrieve participants with user information
        /// - Order by role and lead status
        /// - Handle empty results
        /// </summary>
        public async Task<IEnumerable<InterviewParticipant>> GetInterviewParticipantsAsync(Guid interviewId)
        {
            try
            {
                _logger.LogDebug("Getting participants for interview {InterviewId}", interviewId);

                // 1. Validate interview exists
                var interview = await GetAndValidateInterviewAsync(interviewId, "participant retrieval");

                // 2. Get participants with user details (repository already includes ParticipantUser)
                var participants = await _participantRepository.GetByInterviewAsync(interviewId);

                // 3. Filter out participants with missing users and order by role hierarchy
                var validParticipants = participants
                    .Where(p => p.ParticipantUser != null)
                    .OrderByDescending(p => p.IsLead)
                    .ThenBy(p => p.Role)
                    .ThenBy(p => p.ParticipantUser?.Email ?? "")
                    .ToList();

                // 4. Log any missing users
                var missingUserCount = participants.Count() - validParticipants.Count;
                if (missingUserCount > 0)
                {
                    _logger.LogWarning("Found {MissingCount} participants with missing user details for interview {InterviewId}",
                        missingUserCount, interviewId);
                }

                _logger.LogDebug("Retrieved {Count} participants for interview {InterviewId}",
                    validParticipants.Count, interviewId);

                return validParticipants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get participants for interview {InterviewId}", interviewId);
                throw;
            }
        }

        #endregion

        #region Round Management

        /// <summary>
        /// Gets the latest interview for a job application
        /// Returns the most recent interview by round number and creation date
        /// </summary>
        public async Task<Interview?> GetLatestInterviewForApplicationAsync(Guid jobApplicationId)
        {
            try
            {
                if (jobApplicationId == Guid.Empty)
                    return null;

                var activeInterviews = await _interviewRepository.GetActiveInterviewsByApplicationAsync(jobApplicationId);

                if (!activeInterviews.Any())
                    return null;

                // Get latest by round number first, then by creation date
                var latestInterview = activeInterviews
                    .OrderByDescending(i => i.RoundNumber)
                    .ThenByDescending(i => i.CreatedAt)
                    .First();

                return latestInterview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get latest interview for application {ApplicationId}", jobApplicationId);
                return null;
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Checks if an interview can be scheduled for a job application
        /// Validates application status, existing interviews, and business rules
        /// </summary>
        public async Task<bool> CanScheduleInterviewAsync(Guid jobApplicationId)
        {
            try
            {
                if (jobApplicationId == Guid.Empty)
                    return false;

                // Get job application
                var jobApplication = await _jobApplicationRepository.GetByIdAsync(jobApplicationId);
                if (jobApplication == null || !jobApplication.IsActive)
                    return false;

                // Check application status allows scheduling
                var validStatusesForScheduling = new[] {
                    ApplicationStatus.Shortlisted,
                    ApplicationStatus.Interview,
                    ApplicationStatus.UnderReview,
                    ApplicationStatus.TestCompleted
                };

                if (!validStatusesForScheduling.Contains(jobApplication.Status))
                    return false;

                // Check for pending scheduled interviews
                var activeInterviews = await _interviewRepository.GetActiveInterviewsByApplicationAsync(jobApplicationId);

                // If there are scheduled interviews that haven't been completed, don't allow new scheduling
                var pendingInterview = activeInterviews.FirstOrDefault(i =>
                    i.Status == InterviewStatus.Scheduled &&
                    i.ScheduledDateTime > DateTime.UtcNow);

                if (pendingInterview != null)
                {
                    _logger.LogDebug("Cannot schedule interview - pending interview {InterviewId} exists for application {ApplicationId}",
                        pendingInterview.Id, jobApplicationId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate scheduling eligibility for application {ApplicationId}", jobApplicationId);
                return false;
            }
        }

        /// <summary>
        /// Checks for conflicting interviews for a participant
        /// Validates participant availability considering buffer times and existing interviews
        /// </summary>
        public async Task<bool> HasConflictingInterviewsAsync(Guid participantUserId, DateTime scheduledDateTime, int durationMinutes)
        {
            try
            {
                if (participantUserId == Guid.Empty || durationMinutes <= 0)
                    return false;

                // Get all scheduled interviews for this participant
                var participantInterviews = await _participantRepository.GetByUserAsync(participantUserId);
                var upcomingInterviews = participantInterviews
                    .Where(p => p.Interview.Status == InterviewStatus.Scheduled &&
                               p.Interview.IsActive &&
                               p.Interview.ScheduledDateTime.AddMinutes(p.Interview.DurationMinutes) > DateTime.UtcNow)
                    .Select(p => p.Interview)
                    .ToList();

                if (!upcomingInterviews.Any())
                    return false;

                // Check each interview for conflicts
                var proposedStartTime = scheduledDateTime;
                var proposedEndTime = scheduledDateTime.AddMinutes(durationMinutes);
                const int bufferMinutes = 15; // 15-minute buffer 
                foreach (var existingInterview in upcomingInterviews)
                {
                    var existingStart = existingInterview.ScheduledDateTime;
                    var existingEnd = existingStart.AddMinutes(existingInterview.DurationMinutes);

                    var existingEndWithBuffer = existingEnd.AddMinutes(bufferMinutes);

                    if (proposedStartTime < existingEndWithBuffer && proposedEndTime > existingStart)
                    {
                        _logger.LogWarning("Scheduling conflict detected for participant {ParticipantId}: " +
                            "Proposed {ProposedStart}-{ProposedEnd} conflicts with existing {ExistingStart}-{ExistingEnd} " +
                            "(existing ends at {ExistingEnd}, buffer until {ExistingEndWithBuffer})",
                            participantUserId,
                            scheduledDateTime, proposedEndTime,
                            existingStart, existingEnd,
                            existingEnd, existingEndWithBuffer);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check conflicts for participant {ParticipantId} at {DateTime}",
                    participantUserId, scheduledDateTime);
                return true; // Err on the side of caution
            }
        }

        #endregion

        #region Application Status Management Helper Methods

        /// <summary>
        /// Updates job application status when interview is completed
        /// Updates based on interview outcome
        /// </summary>
        private async Task UpdateApplicationStatusForCompletionAsync(Interview interview, Guid completedByUserId)
        {
            var comments = $"Interview completed: {interview.Title} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
            if (interview.Outcome.HasValue)
            {
                comments += $" - Outcome: {interview.Outcome}";
            }

            await UpdateApplicationStatusAsync(
                interview.JobApplicationId,
                ApplicationStatus.Interview,
                completedByUserId,
                comments,
                "Updated application status for interview completion");
        }

        /// <summary>
        /// Common method for updating job application status with audit trail
        /// </summary>
        private async Task UpdateApplicationStatusAsync(Guid jobApplicationId, ApplicationStatus newStatus, Guid updatedByUserId, string comments, string logMessage)
        {
            try
            {
                var jobApplication = await _jobApplicationRepository.GetByIdAsync(jobApplicationId);
                if (jobApplication == null) return;

                await _jobApplicationRepository.UpdateStatusAsync(
                    jobApplicationId,
                    newStatus,
                    updatedByUserId,
                    comments);

                _logger.LogInformation("{LogMessage} {ApplicationId}", logMessage, jobApplicationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update application status for {ApplicationId}", jobApplicationId);
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Common method to retrieve and validate an interview exists
        /// </summary>
        private async Task<Interview> GetAndValidateInterviewAsync(Guid interviewId, string operation)
        {
            var interview = await _interviewRepository.GetByIdAsync(interviewId);
            if (interview == null)
                throw new InvalidOperationException($"Interview {interviewId} not found for {operation}");

            return interview;
        }



        /// <summary>
        /// Validates the scheduling request and user permissions
        /// Simplified - basic validations only, business rules handled by InterviewService
        /// </summary>
        private async Task ValidateSchedulingRequestAsync(ScheduleInterviewDto dto)
        {
            // Validate job application exists (basic check)
            var jobApplication = await _jobApplicationRepository.GetByIdAsync(dto.JobApplicationId);
            if (jobApplication == null)
                throw new InvalidOperationException($"Job application {dto.JobApplicationId} not found");

            // Validate participants exist (basic check)
            foreach (var participantId in dto.ParticipantUserIds)
            {
                var participant = await _userManager.FindByIdAsync(participantId.ToString());
                if (participant == null)
                    throw new InvalidOperationException($"Participant user {participantId} not found");
            }

        }



        /// <summary>
        /// Determines the next round number for the job application
        /// Uses same logic as InterviewService for consistency
        /// </summary>
        private async Task<int> DetermineNextRoundNumberAsync(Guid jobApplicationId)
        {
            var activeInterviews = await _interviewRepository.GetActiveInterviewsByApplicationAsync(jobApplicationId);

            if (!activeInterviews.Any())
                return 1; // First interview

            // Find the highest round number
            var maxRound = activeInterviews.Max(i => i.RoundNumber);

            // Check if the highest round has been successfully completed with Pass outcome
            var highestRoundInterviews = activeInterviews
                .Where(i => i.RoundNumber == maxRound)
                .ToList();

            var hasSuccessfulCompletion = highestRoundInterviews.Any(i =>
                i.Status == InterviewStatus.Completed &&
                i.Outcome == InterviewOutcome.Pass);

            // If highest round passed, return next round; otherwise allow rescheduling same round
            return hasSuccessfulCompletion ? maxRound + 1 : maxRound;
        }



        /// <summary>
        /// Adds participants to the interview
        /// </summary>
        private async Task AddParticipantsToInterviewAsync(Guid interviewId, IEnumerable<Guid> participantUserIds)
        {
            var participantList = participantUserIds.ToList();
            for (int i = 0; i < participantList.Count; i++)
            {
                var participantUserId = participantList[i];

                // Get the user and interview entities for navigation properties
                var user = await _userManager.FindByIdAsync(participantUserId.ToString());
                var interview = await _interviewRepository.GetByIdAsync(interviewId);

                if (user == null || interview == null)
                    throw new InvalidOperationException($"Cannot create participant - User or Interview not found");

                var participant = new InterviewParticipant
                {
                    InterviewId = interviewId,
                    ParticipantUserId = participantUserId,
                    Role = ParticipantRole.Interviewer, // Default role
                    IsLead = i == 0, // First participant is lead
                    CreatedAt = DateTime.UtcNow,
                    Interview = interview,
                    ParticipantUser = user
                };

                await _participantRepository.CreateAsync(participant);
            }

            _logger.LogInformation("Added {Count} participants to interview {InterviewId}",
                participantList.Count, interviewId);
        }



        /// <summary>
        /// Gets common notification data for an interview
        /// </summary>
        private async Task<(JobApplication? jobApplication, string? candidateName, JobPosition? jobPosition)> GetNotificationDataAsync(Guid jobApplicationId)
        {
            var jobApplication = await _jobApplicationRepository.GetByIdAsync(jobApplicationId);
            if (jobApplication == null) return (null, null, null);

            var candidateProfile = jobApplication.CandidateProfile;
            var jobPosition = jobApplication.JobPosition;
            var candidateName = candidateProfile?.User?.FirstName + " " + candidateProfile?.User?.LastName;

            return (jobApplication, candidateName?.Trim(), jobPosition);
        }



        /// <summary>
        /// Sends notifications for interview scheduling
        /// </summary>
        private async Task SendSchedulingNotificationsAsync(Interview interview, IEnumerable<Guid> participantUserIds)
        {
            try
            {
                var (jobApplication, candidateName, jobPosition) = await GetNotificationDataAsync(interview.JobApplicationId);
                if (jobApplication == null) return;

                // Send notifications to each participant (staff)
                foreach (var participantId in participantUserIds)
                {
                    var participant = await _userManager.FindByIdAsync(participantId.ToString());
                    if (participant?.Email != null)
                    {
                        var participantName = participant.FirstName + " " + participant.LastName;

                        await _emailService.SendInterviewInvitationAsync(
                            participant.Email,
                            participantName?.Trim() ?? "Team Member",
                            candidateName ?? "Candidate",
                            jobPosition?.Title ?? "Position",
                            interview.ScheduledDateTime,
                            interview.DurationMinutes,
                            interview.InterviewType.ToString(),
                            interview.RoundNumber,
                            interview.Mode.ToString(),
                            interview.MeetingDetails,
                            "Interviewer", // Default role for scheduling notifications
                            false); // Not lead in general scheduling
                    }
                }

                // Send notification to candidate
                if (jobApplication?.CandidateProfile?.User?.Email != null)
                {
                    var candidateFullName = jobApplication.CandidateProfile.User.FirstName + " " + jobApplication.CandidateProfile.User.LastName;

                    await _emailService.SendInterviewInvitationAsync(
                        jobApplication.CandidateProfile.User.Email,
                        candidateFullName?.Trim() ?? "Candidate",
                        candidateName ?? "Candidate",
                        jobPosition?.Title ?? "Position",
                        interview.ScheduledDateTime,
                        interview.DurationMinutes,
                        interview.InterviewType.ToString(),
                        interview.RoundNumber,
                        interview.Mode.ToString(),
                        interview.MeetingDetails,
                        "Candidate", // Role for candidate
                        false); // Not lead
                }

                _logger.LogInformation("Sent scheduling notifications for interview {InterviewId}", interview.Id);
            }
            catch (Exception ex)
            {
                // Don't fail the entire operation if notifications fail
                _logger.LogWarning(ex, "Failed to send notifications for interview {InterviewId}", interview.Id);
            }
        }



        /// <summary>
        /// Validates if an interview can be rescheduled
        /// Simplified authorization check
        /// </summary>
        private async Task ValidateInterviewCanBeRescheduledAsync(Interview interview, Guid rescheduledByUserId)
        {
            // Check interview status - cannot reschedule completed, cancelled interviews
            var nonReschedulableStatuses = new[] {
                InterviewStatus.Completed,
                InterviewStatus.Cancelled
            };

            if (nonReschedulableStatuses.Contains(interview.Status))
                throw new InvalidOperationException($"Cannot reschedule interview in {interview.Status} status");

            // Check authorization - simplified
            if (!await CanUserModifyInterviewAsync(interview.JobApplicationId, rescheduledByUserId))
            {
                throw new InvalidOperationException(
                    "You do not have permission to reschedule this interview. Only the assigned recruiter, HR, Admin, or SuperAdmin can reschedule interviews.");
            }

            _logger.LogDebug("Interview {InterviewId} validated for rescheduling by user {UserId}",
                interview.Id, rescheduledByUserId);
        }



        /// <summary>
        /// Checks if user has permission to modify interviews (reschedule, cancel, etc.)
        /// Simple, reusable authorization logic
        /// </summary>
        private async Task<bool> CanUserModifyInterviewAsync(Guid jobApplicationId, Guid userId)
        {
            try
            {
                // Get job application and user info
                var jobApplication = await _jobApplicationRepository.GetByIdAsync(jobApplicationId);
                if (jobApplication == null) return false;

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null) return false;

                // Check if user is the assigned recruiter
                if (jobApplication.AssignedRecruiterId == userId)
                    return true;

                // Check if user has admin roles
                var userRoles = await _userManager.GetRolesAsync(user);
                return userRoles.Any(r => r == "HR" || r == "Admin" || r == "SuperAdmin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user permissions for interview modification");
                return false; // Deny access on error
            }
        }



        /// <summary>
        /// Validates time slot against business rules
        /// Centralized validation logic - THE SINGLE SOURCE OF TRUTH for time slot validation
        /// Used by both InterviewService (creation) and InterviewSchedulingService (rescheduling)
        /// </summary>
        public void ValidateTimeSlot(DateTime scheduledDateTime, int durationMinutes)
        {
            // 1-hour advance notice
            const int minimumAdvanceHours = 1;
            if (scheduledDateTime <= DateTime.UtcNow.AddHours(minimumAdvanceHours))
                throw new ArgumentException($"Interview must be scheduled at least {minimumAdvanceHours} hour(s) in advance");

            // Business hours validation
            const int businessStartHour = 8;
            const int businessEndHour = 18;

            var dayOfWeek = scheduledDateTime.DayOfWeek;
            if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                throw new ArgumentException("Interviews cannot be scheduled on weekends");

            var hour = scheduledDateTime.Hour;
            if (hour < businessStartHour || hour >= businessEndHour)
                throw new ArgumentException($"Interviews must be scheduled between {businessStartHour}:00 AM and {businessEndHour}:00 PM");

            // Ensure interview ends within business hours
            var endTime = scheduledDateTime.AddMinutes(durationMinutes);
            if (endTime.Hour >= businessEndHour && endTime.Minute > 0)
                throw new ArgumentException($"Interview must end by {businessEndHour}:00 PM");
        }



        /// <summary>
        /// Sends rescheduling notifications to all participants
        /// </summary>
        private async Task SendReschedulingNotificationsAsync(Interview interview, DateTime originalDateTime,
            IEnumerable<InterviewParticipant> participants)
        {
            try
            {
                var (jobApplication, candidateName, jobPosition) = await GetNotificationDataAsync(interview.JobApplicationId);
                if (jobApplication == null) return;

                // Send notifications to each participant
                foreach (var participant in participants)
                {
                    if (participant.ParticipantUser?.Email != null)
                    {
                        var participantName = participant.ParticipantUser.FirstName + " " + participant.ParticipantUser.LastName;

                        await _emailService.SendInterviewReschedulingAsync(
                            participant.ParticipantUser.Email,
                            participantName?.Trim() ?? "Team Member",
                            candidateName ?? "Candidate",
                            jobPosition?.Title ?? "Position",
                            originalDateTime,
                            interview.ScheduledDateTime,
                            interview.DurationMinutes,
                            interview.Mode.ToString(),
                            interview.MeetingDetails,
                            false); // Not candidate
                    }
                }

                // Also notify the candidate
                if (jobApplication?.CandidateProfile?.User?.Email != null)
                {
                    var candidateFullName = jobApplication.CandidateProfile.User.FirstName + " " + jobApplication.CandidateProfile.User.LastName;

                    await _emailService.SendInterviewReschedulingAsync(
                        jobApplication.CandidateProfile.User.Email,
                        candidateFullName?.Trim() ?? "Candidate",
                        candidateName ?? "Candidate",
                        jobPosition?.Title ?? "Position",
                        originalDateTime,
                        interview.ScheduledDateTime,
                        interview.DurationMinutes,
                        interview.Mode.ToString(),
                        interview.MeetingDetails,
                        true); // Is candidate
                }

                _logger.LogInformation("Sent rescheduling notifications for interview {InterviewId}", interview.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send rescheduling notifications for interview {InterviewId}", interview.Id);
            }
        }



        /// <summary>
        /// Validates if an interview can be cancelled
        /// Simplified validation using shared authorization logic
        /// </summary>
        private async Task ValidateInterviewCanBeCancelledAsync(Interview interview, Guid cancelledByUserId)
        {
            // Cannot cancel already completed interviews
            if (interview.Status == InterviewStatus.Completed)
                throw new InvalidOperationException("Cannot cancel completed interview");

            // Cannot cancel already cancelled interviews
            if (interview.Status == InterviewStatus.Cancelled)
                throw new InvalidOperationException("Interview is already cancelled");

            // Check authorization using shared method
            if (!await CanUserModifyInterviewAsync(interview.JobApplicationId, cancelledByUserId))
            {
                throw new InvalidOperationException(
                    "You do not have permission to cancel this interview. Only the assigned recruiter, HR, Admin, or SuperAdmin can cancel interviews.");
            }

            _logger.LogDebug("Interview {InterviewId} validated for cancellation by user {UserId}",
                interview.Id, cancelledByUserId);
        }



        /// <summary>
        /// Sends cancellation notifications to all participants and candidate
        /// </summary>
        private async Task SendCancellationNotificationsAsync(Interview interview, IEnumerable<InterviewParticipant> participants, string? reason)
        {
            try
            {
                var (jobApplication, candidateName, jobPosition) = await GetNotificationDataAsync(interview.JobApplicationId);
                if (jobApplication == null) return;

                // Send notifications to each participant
                foreach (var participant in participants)
                {
                    if (participant.ParticipantUser?.Email != null)
                    {
                        var participantName = participant.ParticipantUser.FirstName + " " + participant.ParticipantUser.LastName;

                        await _emailService.SendInterviewCancellationAsync(
                            participant.ParticipantUser.Email,
                            participantName?.Trim() ?? "Team Member",
                            candidateName ?? "Candidate",
                            jobPosition?.Title ?? "Position",
                            interview.ScheduledDateTime,
                            interview.DurationMinutes,
                            interview.RoundNumber,
                            reason,
                            false); // Not candidate
                    }
                }

                // Notify the candidate
                if (jobApplication?.CandidateProfile?.User?.Email != null)
                {
                    var candidateFullName = jobApplication.CandidateProfile.User.FirstName + " " + jobApplication.CandidateProfile.User.LastName;

                    await _emailService.SendInterviewCancellationAsync(
                        jobApplication.CandidateProfile.User.Email,
                        candidateFullName?.Trim() ?? "Candidate",
                        candidateName ?? "Candidate",
                        jobPosition?.Title ?? "Position",
                        interview.ScheduledDateTime,
                        interview.DurationMinutes,
                        interview.RoundNumber,
                        reason,
                        true); // Is candidate
                }

                _logger.LogInformation("Sent cancellation notifications for interview {InterviewId}", interview.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send cancellation notifications for interview {InterviewId}", interview.Id);
            }
        }



        /// <summary>
        /// Sends evaluation reminders to all participants
        /// </summary>
        private async Task SendEvaluationRemindersAsync(Interview interview, IEnumerable<InterviewParticipant> participants)
        {
            try
            {
                var (jobApplication, candidateName, jobPosition) = await GetNotificationDataAsync(interview.JobApplicationId);
                if (jobApplication == null) return;

                // Send evaluation reminders to each participant
                foreach (var participant in participants)
                {
                    if (participant.ParticipantUser?.Email != null)
                    {
                        var participantName = participant.ParticipantUser.FirstName + " " + participant.ParticipantUser.LastName;

                        await _emailService.SendEvaluationReminderAsync(
                            participant.ParticipantUser.Email,
                            participantName?.Trim() ?? "Team Member",
                            candidateName ?? "Candidate",
                            jobPosition?.Title ?? "Position",
                            interview.ScheduledDateTime,
                            interview.RoundNumber,
                            interview.InterviewType.ToString(),
                            interview.DurationMinutes);
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send evaluation reminders for interview {InterviewId}", interview.Id);
            }
        }



        /// <summary>
        /// Validates if an interview can be marked as no-show
        /// </summary>
        private async Task ValidateInterviewCanBeMarkedNoShowAsync(Interview interview, Guid markedByUserId)
        {
            // Interview should be scheduled (simplified - removed InProgress status)
            if (interview.Status != InterviewStatus.Scheduled)
                throw new InvalidOperationException($"Cannot mark interview in {interview.Status} status as no-show - must be Scheduled");

            // Can only mark as no-show after scheduled time + grace period
            var gracePeriodMinutes = 15; // 15-minute grace period
            var noShowThreshold = interview.ScheduledDateTime.AddMinutes(gracePeriodMinutes);

            if (DateTime.UtcNow < noShowThreshold)
                throw new InvalidOperationException($"Cannot mark as no-show before {noShowThreshold:yyyy-MM-dd HH:mm} (15-minute grace period)");
        }



        /// <summary>
        /// Sends no-show notifications
        /// </summary>
        private async Task SendNoShowNotificationsAsync(Interview interview, IEnumerable<InterviewParticipant> participants)
        {
            try
            {
                var (jobApplication, candidateName, jobPosition) = await GetNotificationDataAsync(interview.JobApplicationId);
                if (jobApplication == null) return;

                // Send notifications to each participant
                foreach (var participant in participants)
                {
                    if (participant.ParticipantUser?.Email != null)
                    {
                        var subject = "Interview No-Show Recorded";
                        var body = $@"
                            The interview scheduled for {interview.ScheduledDateTime:yyyy-MM-dd HH:mm} has been marked as a no-show.
                            
                            Interview Details:
                            - Position: {jobPosition?.Title ?? "N/A"}
                            - Candidate: {candidateName ?? "N/A"}
                            - Round: {interview.RoundNumber}
                            
                            Please consider rescheduling options or update the application status accordingly.
                        ";

                        await _emailService.SendEmailAsync(participant.ParticipantUser.Email, subject, body);
                    }
                }

                // Notify HR about the no-show
                // TODO: Add HR notification logic

                _logger.LogInformation("Sent no-show notifications for interview {InterviewId}", interview.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send no-show notifications for interview {InterviewId}", interview.Id);
            }
        }



        /// <summary>
        /// Generates meeting credentials for online interviews using the configured meeting service
        /// </summary>
        private async Task<string?> GenerateMeetingDetailsAsync(Interview interview, List<string> participantEmails)
        {
            try
            {
                // Only generate meeting for online interviews
                if (interview.Mode != InterviewMode.Online)
                {
                    return interview.Mode == InterviewMode.InPerson ? "Meeting room details will be provided separately" : null;
                }

                // Check if meeting service is available
                var isServiceAvailable = await _meetingService.IsServiceAvailableAsync();
                if (!isServiceAvailable)
                {
                    _logger.LogWarning("Meeting service is not available for interview {InterviewId}. Using fallback.", interview.Id);
                    return "Video conference details will be sent via email";
                }

                // Create meeting request
                var meetingRequest = new CreateMeetingRequestDto
                {
                    Title = interview.Title,
                    Description = $"Interview for Round {interview.RoundNumber} - {interview.InterviewType}",
                    StartDateTime = interview.ScheduledDateTime,
                    DurationMinutes = interview.DurationMinutes,
                    OrganizerEmail = _configuration["GoogleMeet:OrganizerEmail"] ?? "recruitment@company.com",
                    AttendeeEmails = participantEmails
                };

                // Create the meeting
                var meetingCredentials = await _meetingService.CreateMeetingAsync(meetingRequest);

                // Format meeting details for storage and notifications
                var meetingDetails = $"Meeting Link: {meetingCredentials.MeetingLink}";

                if (!string.IsNullOrEmpty(meetingCredentials.Password))
                {
                    meetingDetails += $"\nPassword: {meetingCredentials.Password}";
                }

                if (!string.IsNullOrEmpty(meetingCredentials.DialInNumber))
                {
                    meetingDetails += $"\nDial-in: {meetingCredentials.DialInNumber}";
                    if (!string.IsNullOrEmpty(meetingCredentials.AccessCode))
                    {
                        meetingDetails += $" (Access Code: {meetingCredentials.AccessCode})";
                    }
                }

                meetingDetails += $"\nMeeting ID: {meetingCredentials.MeetingId}";

                _logger.LogInformation("Successfully generated {ServiceType} meeting for interview {InterviewId}",
                    _meetingService.GetServiceType(), interview.Id);

                return meetingDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate meeting credentials for interview {InterviewId}", interview.Id);
                return "Video conference link will be provided via email";
            }
        }

        #endregion

    }
}