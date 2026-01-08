using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using System.Linq;

namespace RecruitmentSystem.Services.Implementations
{
    public class InterviewSchedulingService : IInterviewSchedulingService
    {
        #region Dependencies

        private readonly IInterviewService _interviewService;
        private readonly IInterviewRepository _interviewRepository;
        private readonly IInterviewParticipantRepository _participantRepository;
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly IInterviewEvaluationRepository _interviewEvaluationRepository;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly IMeetingService _meetingService;
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
            IInterviewEvaluationRepository interviewEvaluationRepository,
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
            _interviewEvaluationRepository = interviewEvaluationRepository;
            _userManager = userManager;
            _emailService = emailService;
            _meetingService = meetingService;
            _configuration = configuration;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion

        #region Core Scheduling Operations

        public async Task<Interview> ScheduleInterviewAsync(ScheduleInterviewDto dto, Guid scheduledByUserId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dto);

                await ValidateSchedulingRequestAsync(dto, scheduledByUserId);

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

                // Check if this is the first interview for the application
                var existingInterviews = await _interviewRepository.GetActiveInterviewsByApplicationAsync(dto.JobApplicationId);
                var isFirstInterview = !existingInterviews.Any();

                var roundNumber = await DetermineNextRoundNumberAsync(dto.JobApplicationId);

                var createDto = _mapper.Map<CreateInterviewDto>(dto);
                createDto.RoundNumber = roundNumber;
                createDto.ScheduledByUserId = scheduledByUserId;

                var interview = await _interviewService.CreateInterviewAsync(createDto);

                await AddParticipantsToInterviewAsync(interview.Id, dto.ParticipantUserIds);

                // Get participants for meeting setup and notifications
                var participants = await _participantRepository.GetByInterviewAsync(interview.Id);

                if (interview.Mode == InterviewMode.Online)
                {
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

                // Update application status only for the first interview
                if (isFirstInterview)
                {
                    await UpdateApplicationStatusAsync(
                        dto.JobApplicationId,
                        ApplicationStatus.Interview,
                        scheduledByUserId,
                        $"First interview scheduled: {interview.Title} on {interview.ScheduledDateTime:yyyy-MM-dd HH:mm}",
                        "Updated application status to Interview for first interview");
                }
                else
                {
                    // For subsequent interviews in the same round, add a status history without changing status
                    await UpdateApplicationStatusAsync(
                        dto.JobApplicationId,
                        ApplicationStatus.Interview,
                        scheduledByUserId,
                        $"Additional interview scheduled: {interview.Title} on {interview.ScheduledDateTime:yyyy-MM-dd HH:mm}",
                        "Additional interview scheduled in current round");
                }

                await SendInterviewNotificationsAsync(interview, participants, NotificationType.Scheduling);

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
                    if (await HasConflictingInterviewsAsync(participant.ParticipantUserId, dto.NewDateTime, existingInterview.DurationMinutes, interviewId))
                    {
                        var participantUser = await _userManager.FindByIdAsync(participant.ParticipantUserId.ToString());
                        throw new InvalidOperationException(
                            $"Rescheduled time conflicts with existing interviews for participant {participantUser?.Email ?? participant.ParticipantUserId.ToString()}");
                    }
                }

                var originalDateTime = existingInterview.ScheduledDateTime;

                // Cancel existing meeting if it exists (for online interviews)
                if (existingInterview.Mode == InterviewMode.Online && !string.IsNullOrEmpty(existingInterview.MeetingDetails))
                {
                    await CancelExistingMeetingAsync(existingInterview.MeetingDetails, interviewId, "rescheduling");
                }

                // Update only the scheduled date time, preserving all other fields
                existingInterview.ScheduledDateTime = dto.NewDateTime;
                var updatedInterview = await _interviewRepository.UpdateAsync(existingInterview);

                // Generate new meeting details for online interviews with updated time
                if (updatedInterview.Mode == InterviewMode.Online)
                {
                    var participantEmails = participants
                        .Where(p => !string.IsNullOrEmpty(p.ParticipantUser?.Email))
                        .Select(p => p.ParticipantUser!.Email!)
                        .ToList();

                    var jobApplication = await _jobApplicationRepository.GetByIdAsync(updatedInterview.JobApplicationId);
                    if (!string.IsNullOrEmpty(jobApplication?.CandidateProfile?.User?.Email))
                    {
                        participantEmails.Add(jobApplication.CandidateProfile.User.Email);
                    }

                    var newMeetingDetails = await GenerateMeetingDetailsAsync(updatedInterview, participantEmails);

                    if (!string.IsNullOrEmpty(newMeetingDetails))
                    {
                        updatedInterview.MeetingDetails = newMeetingDetails;
                        await _interviewRepository.UpdateAsync(updatedInterview);
                    }
                }

                var reschedulingNote = $"Rescheduled from {originalDateTime:yyyy-MM-dd HH:mm} to {dto.NewDateTime:yyyy-MM-dd HH:mm}. Reason: {dto.Reason ?? "Not specified"}";
                var summaryNotes = string.IsNullOrEmpty(updatedInterview.SummaryNotes)
                    ? reschedulingNote
                    : $"{updatedInterview.SummaryNotes}\n{reschedulingNote}";

                updatedInterview.SummaryNotes = summaryNotes;
                updatedInterview = await _interviewRepository.UpdateAsync(updatedInterview);

                await SendInterviewNotificationsAsync(updatedInterview, participants, NotificationType.Rescheduling, originalDateTime);

                return updatedInterview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reschedule interview {InterviewId}", interviewId);
                throw;
            }
        }

        public async Task<Interview> CancelInterviewAsync(Guid interviewId, CancelInterviewDto dto, Guid cancelledByUserId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dto);

                var existingInterview = await GetAndValidateInterviewAsync(interviewId, "cancellation");

                await ValidateInterviewCanBeCancelledAsync(existingInterview, cancelledByUserId);

                var participants = await _participantRepository.GetByInterviewAsync(interviewId);

                // Cancel the meeting if it exists (for online interviews)
                if (existingInterview.Mode == InterviewMode.Online && !string.IsNullOrEmpty(existingInterview.MeetingDetails))
                {
                    await CancelExistingMeetingAsync(existingInterview.MeetingDetails, interviewId, "cancellation");
                }

                existingInterview.Status = InterviewStatus.Cancelled;
                var cancellationNote = $"Interview cancelled on {DateTime.UtcNow:yyyy-MM-dd HH:mm}. Reason: {dto.Reason ?? "Not specified"}";
                existingInterview.SummaryNotes = string.IsNullOrEmpty(existingInterview.SummaryNotes)
                    ? cancellationNote
                    : $"{existingInterview.SummaryNotes}\n{cancellationNote}";

                var updatedInterview = await _interviewRepository.UpdateAsync(existingInterview);

                await SendInterviewNotificationsAsync(updatedInterview, participants, NotificationType.Cancellation, dto.Reason);

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

        public async Task<Interview> MarkInterviewAsCompletedAsync(Guid interviewId, MarkInterviewCompletedDto dto, Guid completedByUserId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dto);

                var existingInterview = await GetAndValidateInterviewAsync(interviewId, "completion");

                if (!await CanUserCompleteInterviewAsync(existingInterview, completedByUserId))
                {
                    throw new InvalidOperationException(
                        "User does not have permission to mark this interview as completed.");
                }

                if (existingInterview.Status != InterviewStatus.Scheduled)
                    throw new InvalidOperationException($"Cannot complete interview in {existingInterview.Status} status - must be Scheduled");

                if (existingInterview.ScheduledDateTime.AddMinutes(10) > DateTime.UtcNow)
                    throw new InvalidOperationException("Cannot complete interview before scheduled time plus 10 minutes");

                var participants = await _participantRepository.GetByInterviewAsync(interviewId);

                existingInterview.Status = InterviewStatus.Completed;
                var completionNote = $"Interview completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm} marked by {completedByUserId}";

                if (!string.IsNullOrEmpty(dto.SummaryNotes))
                {
                    completionNote += $"\nSummary: {dto.SummaryNotes}";
                }

                existingInterview.SummaryNotes = string.IsNullOrEmpty(existingInterview.SummaryNotes)
                    ? completionNote
                    : $"{existingInterview.SummaryNotes}\n{completionNote}";

                var updatedInterview = await _interviewRepository.UpdateAsync(existingInterview);

                await UpdateApplicationStatusForCompletionAsync(updatedInterview, completedByUserId);

                await SendInterviewNotificationsAsync(updatedInterview, participants, NotificationType.Evaluation);

                return updatedInterview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark interview as completed: {InterviewId}", interviewId);
                throw;
            }
        }

        public async Task<Interview> MarkNoShowAsync(Guid interviewId, MarkInterviewNoShowDto dto, Guid markedByUserId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dto);

                var existingInterview = await GetAndValidateInterviewAsync(interviewId, "no-show marking");

                // Validate user authorization to mark interview as no-show (includes participant permissions)
                if (!await CanUserCompleteInterviewAsync(existingInterview, markedByUserId))
                {
                    throw new InvalidOperationException(
                        "User does not have permission to mark this interview as no-show.");
                }

                ValidateInterviewCanBeMarkedNoShowAsync(existingInterview);

                var participants = await _participantRepository.GetByInterviewAsync(interviewId);

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

                await SendInterviewNotificationsAsync(updatedInterview, participants, NotificationType.NoShow);

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

        public async Task<IEnumerable<InterviewParticipant>> GetInterviewParticipantsAsync(Guid interviewId, Guid requestingUserId)
        {
            try
            {
                var interview = await GetAndValidateInterviewAsync(interviewId, "participant retrieval");

                if (!await CanUserViewInterviewAsync(interview, requestingUserId))
                {
                    throw new UnauthorizedAccessException("You do not have permission to view participants for this interview.");
                }

                var participants = await _participantRepository.GetByInterviewAsync(interviewId);

                var validParticipants = participants
                    .Where(p => p.ParticipantUser != null)
                    .OrderByDescending(p => p.IsLead)
                    .ThenBy(p => p.Role)
                    .ThenBy(p => p.ParticipantUser?.Email ?? "")
                    .ToList();

                var missingUserCount = participants.Count() - validParticipants.Count;
                if (missingUserCount > 0)
                {
                    _logger.LogWarning("Found {MissingCount} participants with missing user details for interview {InterviewId}",
                        missingUserCount, interviewId);
                }

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

        public async Task<bool> HasConflictingInterviewsAsync(Guid participantUserId, DateTime scheduledDateTime, int durationMinutes, Guid? excludeInterviewId = null)
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
                               p.Interview.ScheduledDateTime.AddMinutes(p.Interview.DurationMinutes) > DateTime.UtcNow &&
                               (!excludeInterviewId.HasValue || p.Interview.Id != excludeInterviewId.Value))
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

        public async Task<IEnumerable<AvailableTimeSlotDto>> GetAvailableTimeSlotsAsync(GetAvailableTimeSlotsRequestDto request)
        {
            try
            {
                var availableSlots = new List<AvailableTimeSlotDto>();

                // Get business hours from configuration (stored as UTC)
                var businessTimeZone = _configuration["AppSettings:BusinessTimeZone"] ?? "UTC";
                var utcBusinessStartHour = double.Parse(_configuration["AppSettings:BusinessStartHourUtc"] ?? "9");
                var utcBusinessEndHour = double.Parse(_configuration["AppSettings:BusinessEndHourUtc"] ?? "18");

                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(businessTimeZone);

                var slotIntervalMinutes = 30; // Slot suggestions (user can schedule at any time)

                var participantUsers = await GetParticipantUserDetailsAsync(request.ParticipantUserIds);

                var participantInterviews = await GetParticipantScheduledInterviewsAsync(
                    request.ParticipantUserIds,
                    request.StartDate.Date,
                    request.EndDate.Date.AddDays(1).AddTicks(-1),
                    request.ExcludeJobApplicationId);

                var currentDate = request.StartDate.Date;
                var endDate = request.EndDate.Date;

                while (currentDate <= endDate)
                {
                    // Convert date to business timezone to check for weekends
                    var dateTimeUtc = currentDate;
                    var dateTimeInBusinessTz = TimeZoneInfo.ConvertTimeFromUtc(dateTimeUtc, timeZoneInfo);
                    if (dateTimeInBusinessTz.DayOfWeek != DayOfWeek.Saturday && dateTimeInBusinessTz.DayOfWeek != DayOfWeek.Sunday)
                    {
                        var currentSlotTime = currentDate.AddHours(utcBusinessStartHour);
                        var dayEndTime = currentDate.AddHours(utcBusinessEndHour);

                        while (currentSlotTime.AddMinutes(request.DurationMinutes) <= dayEndTime)
                        {
                            if (currentSlotTime > DateTime.UtcNow.AddHours(1))
                            {
                                var slotEndTime = currentSlotTime.AddMinutes(request.DurationMinutes);
                                var (available, unavailable) = CheckParticipantAvailabilityForSlot(
                                    currentSlotTime,
                                    slotEndTime,
                                    participantUsers,
                                    participantInterviews);

                                // Include slot if at least one participant is available
                                if (available.Count > 0)
                                {
                                    var slot = new AvailableTimeSlotDto
                                    {
                                        StartDateTime = currentSlotTime,
                                        EndDateTime = slotEndTime,
                                        DurationMinutes = request.DurationMinutes,
                                        IsRecommended = false,
                                        AvailableParticipants = available,
                                        UnavailableParticipants = unavailable
                                    };

                                    availableSlots.Add(slot);
                                }
                            }

                            currentSlotTime = currentSlotTime.AddMinutes(slotIntervalMinutes);
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                }

                _logger.LogInformation("Generated {Count} available time slots for date range {StartDate} to {EndDate}",
                    availableSlots.Count, request.StartDate, request.EndDate);

                return availableSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available time slots for date range {StartDate} to {EndDate}",
                    request.StartDate, request.EndDate);
                throw;
            }
        }

        private async Task<Dictionary<Guid, string>> GetParticipantUserDetailsAsync(List<Guid> participantUserIds)
        {
            var userDetails = new Dictionary<Guid, string>();

            if (participantUserIds == null || !participantUserIds.Any())
                return userDetails;

            // Batch load all users at once to avoid N+1 queries
            var users = await Task.Run(() => _userManager.Users
                .Where(u => participantUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToList());

            foreach (var user in users)
            {
                userDetails[user.Id] = $"{user.FirstName} {user.LastName}";
            }

            return userDetails;
        }

        private async Task<Dictionary<Guid, List<Interview>>> GetParticipantScheduledInterviewsAsync(
            List<Guid> participantUserIds,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeJobApplicationId)
        {
            var participantInterviews = new Dictionary<Guid, List<Interview>>();

            if (participantUserIds == null || !participantUserIds.Any())
                return participantInterviews;

            var allInterviewsInRange = await _interviewRepository.GetByDateRangeAsync(
                startDate.AddDays(-1),
                endDate.AddDays(1),
                includeBasicDetails: false);

            var scheduledInterviews = allInterviewsInRange
                .Where(i => i.Status == InterviewStatus.Scheduled)
                .Where(i => excludeJobApplicationId == null || i.JobApplicationId != excludeJobApplicationId.Value)
                .ToList();

            foreach (var userId in participantUserIds)
            {
                var userInterviews = await _participantRepository.GetInterviewIdsByUserAsync(userId);
                var userInterviewSet = new HashSet<Guid>(userInterviews);

                var relevantInterviews = scheduledInterviews
                    .Where(i => userInterviewSet.Contains(i.Id))
                    .ToList();

                participantInterviews[userId] = relevantInterviews;
            }

            return participantInterviews;
        }

        private (List<string> available, List<string> unavailable) CheckParticipantAvailabilityForSlot(
            DateTime slotStart,
            DateTime slotEnd,
            Dictionary<Guid, string> participantUsers,
            Dictionary<Guid, List<Interview>> participantInterviews)
        {
            var available = new List<string>();
            var unavailable = new List<string>();
            const int bufferMinutes = 15;

            foreach (var (userId, userName) in participantUsers)
            {
                var hasConflict = false;

                if (participantInterviews.TryGetValue(userId, out var interviews))
                {
                    foreach (var interview in interviews)
                    {
                        var interviewEnd = interview.ScheduledDateTime.AddMinutes(interview.DurationMinutes);
                        var interviewEndWithBuffer = interviewEnd.AddMinutes(bufferMinutes);

                        if (slotStart < interviewEndWithBuffer && slotEnd > interview.ScheduledDateTime)
                        {
                            hasConflict = true;
                            break;
                        }
                    }
                }

                if (hasConflict)
                {
                    unavailable.Add(userName);
                }
                else
                {
                    available.Add(userName);
                }
            }

            return (available, unavailable);
        }

        #endregion

        #region Private Helper Methods

        private async Task<Interview> GetAndValidateInterviewAsync(Guid interviewId, string operation)
        {
            var interview = await _interviewRepository.GetByIdAsync(interviewId);
            if (interview == null)
                throw new InvalidOperationException($"Interview {interviewId} not found for {operation}");

            return interview;
        }



        private async Task ValidateSchedulingRequestAsync(ScheduleInterviewDto dto, Guid scheduledByUserId)
        {
            // Validate job application exists
            var jobApplication = await _jobApplicationRepository.GetByIdAsync(dto.JobApplicationId);
            if (jobApplication == null)
                throw new InvalidOperationException($"Job application {dto.JobApplicationId} not found");

            if (!await HasModifyPermissionsAsync(jobApplication, scheduledByUserId))
            {
                throw new InvalidOperationException(
                    "User does not have permission to schedule interviews for this application");
            }

            // Validate participants exist
            foreach (var participantId in dto.ParticipantUserIds)
            {
                var participant = await _userManager.FindByIdAsync(participantId.ToString());
                if (participant == null)
                    throw new InvalidOperationException($"Participant user {participantId} not found");
            }

        }



        private async Task<int> DetermineNextRoundNumberAsync(Guid jobApplicationId)
        {
            var activeInterviews = await _interviewRepository.GetActiveInterviewsByApplicationAsync(jobApplicationId);

            if (!activeInterviews.Any())
                return 1; // First interview

            var maxRound = activeInterviews.Max(i => i.RoundNumber);

            var highestRoundInterviews = activeInterviews
                .Where(i => i.RoundNumber == maxRound)
                .ToList();

            var hasCompletedInterview = highestRoundInterviews.Any(i =>
                i.Status == InterviewStatus.Completed);

            return hasCompletedInterview ? maxRound + 1 : maxRound;
        }



        private async Task AddParticipantsToInterviewAsync(Guid interviewId, IEnumerable<Guid> participantUserIds)
        {
            var participantList = participantUserIds.ToList();
            for (int i = 0; i < participantList.Count; i++)
            {
                var participantUserId = participantList[i];

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



        private async Task<(JobApplication? jobApplication, string? candidateName, JobPosition? jobPosition)> GetNotificationDataAsync(Guid jobApplicationId)
        {
            var jobApplication = await _jobApplicationRepository.GetByIdAsync(jobApplicationId);
            if (jobApplication == null) return (null, null, null);

            var candidateProfile = jobApplication.CandidateProfile;
            var jobPosition = jobApplication.JobPosition;
            var candidateName = candidateProfile?.User?.FirstName + " " + candidateProfile?.User?.LastName;

            return (jobApplication, candidateName?.Trim(), jobPosition);
        }



        private async Task ValidateInterviewCanBeRescheduledAsync(Interview interview, Guid rescheduledByUserId)
        {
            var nonReschedulableStatuses = new[] {
                InterviewStatus.Completed,
                InterviewStatus.Cancelled
            };

            if (nonReschedulableStatuses.Contains(interview.Status))
                throw new InvalidOperationException($"Cannot reschedule interview in {interview.Status} status");

            var jobApplication = await _jobApplicationRepository.GetByIdAsync(interview.JobApplicationId);
            if (jobApplication == null)
                throw new InvalidOperationException($"Job application {interview.JobApplicationId} not found");

            if (!await HasModifyPermissionsAsync(jobApplication, rescheduledByUserId))
            {
                throw new InvalidOperationException(
                    "You do not have permission to reschedule this interview. Only the assigned recruiter, HR, Admin, or SuperAdmin can reschedule interviews.");
            }
        }



        private async Task<bool> CanUserCompleteInterviewAsync(Interview interview, Guid userId)
        {
            try
            {
                var jobApplication = await _jobApplicationRepository.GetByIdAsync(interview.JobApplicationId);
                if (jobApplication == null) return false;

                if (await HasModifyPermissionsAsync(jobApplication, userId))
                    return true;

                var isParticipant = await _participantRepository.IsUserParticipantInInterviewAsync(interview.Id, userId);
                return isParticipant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user permissions for interview completion");
                return false; // Deny access on error
            }
        }

        private async Task<bool> CanUserViewInterviewAsync(Interview interview, Guid userId)
        {
            try
            {
                var jobApplication = await _jobApplicationRepository.GetByIdAsync(interview.JobApplicationId);
                if (jobApplication == null) return false;

                if (await HasModifyPermissionsAsync(jobApplication, userId))
                    return true;

                var isParticipant = await _participantRepository.IsUserParticipantInInterviewAsync(interview.Id, userId);
                if (isParticipant) return true;

                if (jobApplication.CandidateProfile?.UserId == userId)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user permissions for interview viewing");
                return false; // Deny access on error
            }
        }

        private async Task<bool> HasModifyPermissionsAsync(JobApplication jobApplication, Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null) return false;

                if (jobApplication.AssignedRecruiterId == userId)
                    return true;

                var userRoles = await _userManager.GetRolesAsync(user);
                return userRoles.Any(r => r == "HR" || r == "Admin" || r == "SuperAdmin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking modify permissions");
                return false; // Deny access on error
            }
        }



        public void ValidateTimeSlot(DateTime scheduledDateTime, int durationMinutes)
        {
            const int minimumAdvanceHours = 1;
            if (scheduledDateTime <= DateTime.UtcNow.AddHours(minimumAdvanceHours))
                throw new ArgumentException($"Interview must be scheduled at least {minimumAdvanceHours} hour(s) in advance");

            // Get business hours from configuration (stored as UTC)
            var businessTimeZone = _configuration["AppSettings:BusinessTimeZone"] ?? "UTC";
            var utcBusinessStartHour = double.Parse(_configuration["AppSettings:BusinessStartHourUtc"] ?? "9");
            var utcBusinessEndHour = double.Parse(_configuration["AppSettings:BusinessEndHourUtc"] ?? "18");

            // Convert scheduled time to business timezone for weekend validation
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(businessTimeZone);
            var scheduledTimeInBusinessTz = TimeZoneInfo.ConvertTimeFromUtc(scheduledDateTime, timeZoneInfo);

            var dayOfWeek = scheduledTimeInBusinessTz.DayOfWeek;
            if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                throw new ArgumentException("Interviews cannot be scheduled on weekends");

            // Calculate hour as decimal (e.g., 4:30 = 4.5)
            var hourDecimal = scheduledDateTime.Hour + (scheduledDateTime.Minute / 60.0);
            if (hourDecimal < utcBusinessStartHour || hourDecimal >= utcBusinessEndHour)
                throw new ArgumentException($"Interviews must be scheduled between {utcBusinessStartHour:0.##} and {utcBusinessEndHour:0.##} UTC");

            var endTime = scheduledDateTime.AddMinutes(durationMinutes);
            var endHourDecimal = endTime.Hour + (endTime.Minute / 60.0);
            if (endHourDecimal > utcBusinessEndHour)
                throw new ArgumentException($"Interview must end by {utcBusinessEndHour:0.##} UTC");
        }

        #region Automation Helpers

        public async Task<int> SendUpcomingInterviewRemindersAsync(int hoursAhead = 4)
        {
            hoursAhead = hoursAhead < 1 ? 1 : hoursAhead;

            var now = DateTime.UtcNow;
            var windowStart = hoursAhead > 1 ? now.AddHours(hoursAhead - 1) : now;
            var windowEnd = now.AddHours(hoursAhead);

            var interviews = await _interviewRepository.GetByDateRangeAsync(windowStart, windowEnd, includeBasicDetails: true);
            var remindersSent = 0;

            foreach (var interview in interviews)
            {
                if (interview.Status != InterviewStatus.Scheduled)
                {
                    continue;
                }

                var timeUntilStart = interview.ScheduledDateTime - now;
                if (timeUntilStart.TotalMinutes <= 0 || timeUntilStart.TotalHours > hoursAhead)
                {
                    continue;
                }

                if (hoursAhead > 1 && timeUntilStart.TotalHours < hoursAhead - 1)
                {
                    continue;
                }

                var participants = (await _participantRepository.GetByInterviewAsync(interview.Id)).ToList();
                if (!participants.Any())
                {
                    continue;
                }

                var reminderMessage = $"Reminder: Interview '{interview.Title}' starts at {interview.ScheduledDateTime:yyyy-MM-dd HH:mm} ({Math.Max(1, (int)Math.Ceiling(timeUntilStart.TotalMinutes / 60d))} hour(s) remaining).";

                await SendInterviewNotificationsAsync(interview, participants, NotificationType.Reminder, reminderMessage);
                remindersSent++;
            }

            if (remindersSent > 0)
            {
                _logger.LogInformation("Sent {Count} upcoming interview reminders (lookahead {HoursAhead}h)", remindersSent, hoursAhead);
            }

            return remindersSent;
        }

        public async Task<int> SendPendingEvaluationRemindersAsync(int hoursAfterCompletion = 24)
        {
            hoursAfterCompletion = hoursAfterCompletion < 1 ? 1 : hoursAfterCompletion;

            const int scheduleFrequencyHours = 1;
            var windowEnd = DateTime.UtcNow.AddHours(-hoursAfterCompletion);
            var windowStart = windowEnd.AddHours(-scheduleFrequencyHours);

            var completedInterviews = await _interviewRepository.GetCompletedInterviewsInDateRangeAsync(
                windowStart,
                windowEnd,
                includeDetails: true);

            if (!completedInterviews.Any())
            {
                return 0;
            }

            var remindersSent = 0;

            foreach (var interview in completedInterviews)
            {
                var participants = interview.Participants?.ToList() ?? new List<InterviewParticipant>();
                if (!participants.Any())
                {
                    continue;
                }

                foreach (var participant in participants)
                {
                    if (participant.ParticipantUserId == Guid.Empty)
                    {
                        continue;
                    }

                    var hasSubmitted = await _interviewEvaluationRepository.HasEvaluatorSubmittedAsync(
                        interview.Id,
                        participant.ParticipantUserId);

                    if (hasSubmitted)
                    {
                        continue;
                    }

                    var evaluatorEmail = participant.ParticipantUser?.Email;
                    if (string.IsNullOrWhiteSpace(evaluatorEmail))
                    {
                        continue;
                    }

                    var participantName = $"{participant.ParticipantUser?.FirstName} {participant.ParticipantUser?.LastName}".Trim();
                    var candidateName = interview.JobApplication?.CandidateProfile?.User != null
                        ? $"{interview.JobApplication.CandidateProfile.User.FirstName} {interview.JobApplication.CandidateProfile.User.LastName}".Trim()
                        : "Candidate";
                    var jobTitle = interview.JobApplication?.JobPosition?.Title ?? "the position";

                    var sent = await _emailService.SendEvaluationReminderAsync(
                        evaluatorEmail,
                        string.IsNullOrWhiteSpace(participantName) ? "Interviewer" : participantName,
                        string.IsNullOrWhiteSpace(candidateName) ? "Candidate" : candidateName,
                        jobTitle,
                        interview.ScheduledDateTime,
                        interview.RoundNumber,
                        interview.InterviewType.ToString(),
                        interview.DurationMinutes);

                    if (sent)
                    {
                        remindersSent++;
                    }
                }
            }

            if (remindersSent > 0)
            {
                _logger.LogInformation(
                    "Sent {Count} evaluation follow-up reminders ({Hours}h threshold; window {WindowStart:o} - {WindowEnd:o})",
                    remindersSent,
                    hoursAfterCompletion,
                    windowStart,
                    windowEnd);
            }

            return remindersSent;
        }

        #endregion



        private async Task SendInterviewNotificationsAsync(
            Interview interview,
            IEnumerable<InterviewParticipant> participants,
            NotificationType notificationType,
            object? additionalData = null)
        {
            try
            {
                var (jobApplication, candidateName, jobPosition) = await GetNotificationDataAsync(interview.JobApplicationId);
                if (jobApplication == null) return;

                foreach (var participant in participants)
                {
                    if (participant.ParticipantUser?.Email != null)
                    {
                        var participantName = participant.ParticipantUser.FirstName + " " + participant.ParticipantUser.LastName;

                        await SendParticipantNotificationAsync(
                            participant.ParticipantUser.Email,
                            participantName?.Trim() ?? "Team Member",
                            candidateName ?? "Candidate",
                            jobPosition?.Title ?? "Position",
                            interview,
                            notificationType,
                            additionalData,
                            false,
                            participant.IsLead); // Not candidate
                    }
                }

                if (ShouldNotifyCandidate(notificationType) && jobApplication?.CandidateProfile?.User?.Email != null)
                {
                    var candidateFullName = jobApplication.CandidateProfile.User.FirstName + " " + jobApplication.CandidateProfile.User.LastName;

                    await SendParticipantNotificationAsync(
                        jobApplication.CandidateProfile.User.Email,
                        candidateFullName?.Trim() ?? "Candidate",
                        candidateName ?? "Candidate",
                        jobPosition?.Title ?? "Position",
                        interview,
                        notificationType,
                        additionalData,
                        true,
                        false); // Is candidate
                }

                _logger.LogInformation("Sent {NotificationType} notifications for interview {InterviewId}",
                    notificationType, interview.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send {NotificationType} notifications for interview {InterviewId}",
                    notificationType, interview.Id);
            }
        }

        private bool ShouldNotifyCandidate(NotificationType notificationType)
        {
            return notificationType switch
            {
                NotificationType.Scheduling => true,
                NotificationType.Rescheduling => true,
                NotificationType.Cancellation => true,
                NotificationType.NoShow => false, // HR only for no-show
                NotificationType.Evaluation => false, // Participants only
                NotificationType.Reminder => true,
                _ => false
            };
        }

        private async Task SendParticipantNotificationAsync(
            string email,
            string recipientName,
            string candidateName,
            string positionTitle,
            Interview interview,
            NotificationType notificationType,
            object? additionalData,
            bool isCandidate,
            bool isLead,
            string? instructions = null)
        {
            switch (notificationType)
            {
                case NotificationType.Scheduling:
                    await _emailService.SendInterviewInvitationAsync(
                        email, recipientName, candidateName, positionTitle,
                        interview.ScheduledDateTime, interview.DurationMinutes,
                        interview.InterviewType.ToString(), interview.RoundNumber,
                        interview.Mode.ToString(), interview.MeetingDetails,
                        isCandidate ? "Candidate" : "Interviewer", isLead, instructions);
                    break;

                case NotificationType.Rescheduling:
                    var originalDateTime = additionalData as DateTime?;
                    if (originalDateTime.HasValue)
                    {
                        await _emailService.SendInterviewReschedulingAsync(
                            email, recipientName, candidateName, positionTitle,
                            originalDateTime.Value, interview.ScheduledDateTime,
                            interview.DurationMinutes, interview.Mode.ToString(),
                            interview.MeetingDetails, isCandidate);
                    }
                    break;

                case NotificationType.Cancellation:
                    var reason = additionalData as string;
                    await _emailService.SendInterviewCancellationAsync(
                        email, recipientName, candidateName, positionTitle,
                        interview.ScheduledDateTime, interview.DurationMinutes,
                        interview.RoundNumber, reason, isCandidate);
                    break;

                case NotificationType.Evaluation:
                    await _emailService.SendEvaluationReminderAsync(
                        email, recipientName, candidateName, positionTitle,
                        interview.ScheduledDateTime, interview.RoundNumber,
                        interview.InterviewType.ToString(), interview.DurationMinutes);
                    break;

                case NotificationType.Reminder:
                    var reminderNote = additionalData as string;
                    await _emailService.SendInterviewInvitationAsync(
                        email, recipientName, candidateName, positionTitle,
                        interview.ScheduledDateTime, interview.DurationMinutes,
                        interview.InterviewType.ToString(), interview.RoundNumber,
                        interview.Mode.ToString(), interview.MeetingDetails,
                        isCandidate ? "Candidate" : "Interviewer", isLead, reminderNote ?? "Reminder: upcoming interview");
                    break;

                case NotificationType.NoShow:
                    var subject = "Interview No-Show Recorded";
                    var body = $@"
                        The interview scheduled for {interview.ScheduledDateTime:yyyy-MM-dd HH:mm} has been marked as a no-show.

                        Interview Details:
                        - Position: {positionTitle}
                        - Candidate: {candidateName}
                        - Round: {interview.RoundNumber}

                        Please consider rescheduling options or update the application status accordingly.
                    ";
                    await _emailService.SendEmailAsync(email, subject, body);
                    break;
            }
        }

        private enum NotificationType
        {
            Scheduling,
            Rescheduling,
            Cancellation,
            NoShow,
            Evaluation,
            Reminder
        }



        private async Task ValidateInterviewCanBeCancelledAsync(Interview interview, Guid cancelledByUserId)
        {
            if (interview.Status != InterviewStatus.Scheduled)
                throw new InvalidOperationException("Cannot cancel interview that is not in Scheduled status");

            var jobApplication = await _jobApplicationRepository.GetByIdAsync(interview.JobApplicationId);
            if (jobApplication == null)
                throw new InvalidOperationException($"Job application {interview.JobApplicationId} not found");

            if (!await HasModifyPermissionsAsync(jobApplication, cancelledByUserId))
            {
                throw new InvalidOperationException(
                    "You do not have permission to cancel this interview. Only the assigned recruiter, HR, Admin, or SuperAdmin can cancel interviews.");
            }
        }



        private void ValidateInterviewCanBeMarkedNoShowAsync(Interview interview)
        {
            if (interview.Status != InterviewStatus.Scheduled)
                throw new InvalidOperationException($"Cannot mark interview in {interview.Status} status as no-show - must be Scheduled");

            var gracePeriodMinutes = 15; // 15-minute grace period
            var noShowThreshold = interview.ScheduledDateTime.AddMinutes(gracePeriodMinutes);

            if (DateTime.UtcNow < noShowThreshold)
                throw new InvalidOperationException($"Cannot mark as no-show before {noShowThreshold:yyyy-MM-dd HH:mm} (15-minute grace period)");
        }



        private string? ExtractMeetingIdFromDetails(string? meetingDetails)
        {
            if (string.IsNullOrEmpty(meetingDetails))
                return null;

            try
            {
                const string meetingIdPrefix = "Meeting ID: ";
                var lines = meetingDetails.Split('\n');

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith(meetingIdPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var meetingId = trimmedLine.Substring(meetingIdPrefix.Length).Trim();
                        if (!string.IsNullOrEmpty(meetingId))
                        {
                            return meetingId;
                        }
                    }
                }

                _logger.LogWarning("No meeting ID found in meeting details: {MeetingDetails}", meetingDetails);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting meeting ID from details: {MeetingDetails}", meetingDetails);
                return null;
            }
        }

        private async Task CancelExistingMeetingAsync(string? meetingDetails, Guid interviewId, string operation)
        {
            try
            {
                var meetingId = ExtractMeetingIdFromDetails(meetingDetails);
                if (string.IsNullOrEmpty(meetingId))
                {
                    return;
                }

                // Check if meeting service is available
                var isServiceAvailable = await _meetingService.IsServiceAvailableAsync();
                if (!isServiceAvailable)
                {
                    _logger.LogWarning("Meeting service not available, cannot cancel meeting {MeetingId} for interview {InterviewId}",
                        meetingId, interviewId);
                    return;
                }

                var cancelResult = await _meetingService.CancelMeetingAsync(meetingId);

                if (cancelResult)
                {
                    _logger.LogInformation("Successfully cancelled meeting {MeetingId} for interview {InterviewId} during {Operation}",
                        meetingId, interviewId, operation);
                }
                else
                {
                    _logger.LogWarning("Failed to cancel meeting {MeetingId} for interview {InterviewId} during {Operation}",
                        meetingId, interviewId, operation);
                }
            }
            catch (Exception ex)
            {
                // Don't fail the entire operation if meeting cancellation fails
                _logger.LogError(ex, "Error cancelling meeting for interview {InterviewId} during {Operation}",
                    interviewId, operation);
            }
        }

        private async Task<string?> GenerateMeetingDetailsAsync(Interview interview, List<string> participantEmails)
        {
            try
            {
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