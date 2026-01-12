using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Entities.Projections;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;

namespace RecruitmentSystem.Services.Implementations
{
    public class InterviewReportingService : IInterviewReportingService
    {
        #region Dependencies

        private readonly IInterviewRepository _interviewRepository;
        private readonly IInterviewParticipantRepository _participantRepository;
        private readonly IInterviewEvaluationRepository _evaluationRepository;
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<InterviewReportingService> _logger;

        #endregion

        #region Constructor

        public InterviewReportingService(
            IInterviewRepository interviewRepository,
            IInterviewParticipantRepository participantRepository,
            IInterviewEvaluationRepository evaluationRepository,
            IJobApplicationRepository jobApplicationRepository,
            IMapper mapper,
            ILogger<InterviewReportingService> logger)
        {
            _interviewRepository = interviewRepository;
            _participantRepository = participantRepository;
            _evaluationRepository = evaluationRepository;
            _jobApplicationRepository = jobApplicationRepository;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion

        #region Analytics and Statistics

        public async Task<Dictionary<InterviewStatus, int>> GetInterviewStatusDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var endDate = toDate ?? DateTime.UtcNow;

                if (startDate > endDate)
                    throw new ArgumentException("From date cannot be after to date.", nameof(fromDate));

                var interviews = await _interviewRepository.GetByDateRangeAsync(startDate, endDate);

                var distribution = interviews
                    .GroupBy(i => i.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (InterviewStatus status in Enum.GetValues(typeof(InterviewStatus)))
                {
                    if (!distribution.ContainsKey(status))
                        distribution[status] = 0;
                }

                return distribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interview status distribution from {FromDate} to {ToDate}",
                    fromDate, toDate);
                throw;
            }
        }

        public async Task<Dictionary<InterviewType, int>> GetInterviewTypeDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var endDate = toDate ?? DateTime.UtcNow;

                if (startDate > endDate)
                    throw new ArgumentException("From date cannot be after to date.", nameof(fromDate));

                var interviews = await _interviewRepository.GetByDateRangeAsync(startDate, endDate);

                var distribution = interviews
                    .GroupBy(i => i.InterviewType)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (InterviewType type in Enum.GetValues(typeof(InterviewType)))
                {
                    if (!distribution.ContainsKey(type))
                        distribution[type] = 0;
                }

                return distribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interview type distribution from {FromDate} to {ToDate}",
                    fromDate, toDate);
                throw;
            }
        }

        public async Task<InterviewAnalyticsDto> GetInterviewAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var endDate = toDate ?? DateTime.UtcNow;

                if (startDate > endDate)
                    throw new ArgumentException("From date cannot be after to date.", nameof(fromDate));

                var interviews = await _interviewRepository.GetByDateRangeAsync(startDate, endDate, includeBasicDetails: true);
                var interviewsList = interviews.ToList();

                var statusDistribution = interviewsList
                    .GroupBy(i => i.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                var typeDistribution = interviewsList
                    .GroupBy(i => i.InterviewType.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                var totalInterviews = interviewsList.Count;
                var upcomingInterviews = interviewsList.Count(i => i.Status == InterviewStatus.Scheduled && i.ScheduledDateTime > DateTime.UtcNow);
                var completedInterviews = interviewsList.Count(i => i.Status == InterviewStatus.Completed);
                var cancelledInterviews = interviewsList.Count(i => i.Status == InterviewStatus.Cancelled);

                var completedInterviewDurations = interviewsList
                    .Where(i => i.Status == InterviewStatus.Completed)
                    .Select(i => (double)i.DurationMinutes)
                    .ToList();

                var averageDuration = completedInterviewDurations.Any()
                    ? completedInterviewDurations.Average()
                    : 0.0;

                var analytics = new InterviewAnalyticsDto
                {
                    StatusDistribution = statusDistribution,
                    TypeDistribution = typeDistribution,
                    TotalInterviews = totalInterviews,
                    UpcomingInterviews = upcomingInterviews,
                    CompletedInterviews = completedInterviews,
                    CancelledInterviews = cancelledInterviews,
                    AverageInterviewDuration = Math.Round(averageDuration, 1)
                };

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comprehensive interview analytics from {FromDate} to {ToDate}",
                    fromDate, toDate);
                throw;
            }
        }

        #endregion

        #region Search and Filtering

        public async Task<PagedResult<InterviewSummaryDto>> SearchInterviewsAsync(InterviewSearchDto searchDto, Guid? userId)
        {
            var pageNumber = searchDto.PageNumber ?? 1;
            var pageSize = searchDto.PageSize ?? 20;

            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(searchDto.PageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(searchDto.PageSize));

            var fetchTask = _interviewRepository.SearchInterviewSummariesAsync(
                userId: userId,
                status: searchDto.Status,
                interviewType: searchDto.InterviewType,
                mode: searchDto.Mode,
                scheduledFromDate: searchDto.ScheduledFromDate,
                scheduledToDate: searchDto.ScheduledToDate,
                participantUserId: searchDto.ParticipantUserId,
                jobApplicationId: searchDto.JobApplicationId,
                pageNumber: pageNumber,
                pageSize: pageSize);

            return await MapSummaryResultAsync<InterviewSummaryDto>(fetchTask, pageNumber, pageSize, "search criteria");
        }

        public async Task<PagedResult<InterviewSummaryDto>> GetUpcomingInterviewsForUserAsync(Guid userId, int days = 7, int pageNumber = 1, int pageSize = 20)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.", nameof(userId));
            if (days < 1 || days > 90)
                throw new ArgumentException("Days must be between 1 and 90.", nameof(days));
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _interviewRepository.GetUpcomingInterviewSummariesForUserAsync(userId, days, pageNumber, pageSize);
            return await MapSummaryResultAsync<InterviewSummaryDto>(fetchTask, pageNumber, pageSize, $"upcoming interviews for user {userId}");
        }

        public async Task<PagedResult<InterviewPublicSummaryDto>> GetPublicUpcomingInterviewsForUserAsync(Guid userId, int days = 7, int pageNumber = 1, int pageSize = 20)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.", nameof(userId));
            if (days < 1 || days > 90)
                throw new ArgumentException("Days must be between 1 and 90.", nameof(days));
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _interviewRepository.GetUpcomingInterviewSummariesForUserAsync(userId, days, pageNumber, pageSize);
            return await MapSummaryResultAsync<InterviewPublicSummaryDto>(fetchTask, pageNumber, pageSize, $"public upcoming interviews for user {userId}");
        }

        public async Task<PagedResult<InterviewSummaryDto>> GetTodayInterviewsAsync(Guid? participantUserId = null, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var today = DateTime.UtcNow.Date;
            var fetchTask = _interviewRepository.GetTodayInterviewSummariesAsync(today, participantUserId, pageNumber, pageSize);

            var scenario = participantUserId.HasValue
                ? $"today's interviews for participant {participantUserId}"
                : "today's interviews";
            return await MapSummaryResultAsync<InterviewSummaryDto>(fetchTask, pageNumber, pageSize, scenario);
        }

        public async Task<PagedResult<InterviewSummaryDto>> GetInterviewsNeedingActionAsync(Guid? userId, bool isPrivilegedStaff, bool isRecruiter, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _interviewRepository.GetInterviewsNeedingActionProjectionAsync(userId, isPrivilegedStaff, isRecruiter, pageNumber, pageSize);
            var scenario = isPrivilegedStaff ? "interviews requiring action" : $"interviews requiring action for user {userId}";
            return await MapNeedingActionResultAsync<InterviewSummaryDto>(fetchTask, pageNumber, pageSize, scenario);
        }

        #endregion

        #region Application-specific Queries

        public async Task<PagedResult<InterviewSummaryDto>> GetInterviewsByApplicationAsync(Guid jobApplicationId, int pageNumber = 1, int pageSize = 20)
        {
            if (jobApplicationId == Guid.Empty)
                throw new ArgumentException("JobApplicationId cannot be empty.", nameof(jobApplicationId));
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _interviewRepository.GetInterviewSummariesByApplicationAsync(jobApplicationId, pageNumber, pageSize);

            return await MapSummaryResultAsync<InterviewSummaryDto>(fetchTask, pageNumber, pageSize, $"application {jobApplicationId}");
        }

        public async Task<PagedResult<InterviewSummaryDto>> GetInterviewsByParticipantAsync(Guid participantUserId, int pageNumber = 1, int pageSize = 20)
        {
            if (participantUserId == Guid.Empty)
                throw new ArgumentException("ParticipantUserId cannot be empty.", nameof(participantUserId));
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _interviewRepository.GetInterviewSummariesByParticipantAsync(participantUserId, pageNumber, pageSize);

            return await MapSummaryResultAsync<InterviewSummaryDto>(fetchTask, pageNumber, pageSize, $"participant {participantUserId}");
        }

        public async Task<PagedResult<InterviewPublicSummaryDto>> GetPublicInterviewsByParticipantAsync(Guid participantUserId, int pageNumber = 1, int pageSize = 20)
        {
            if (participantUserId == Guid.Empty)
                throw new ArgumentException("ParticipantUserId cannot be empty.", nameof(participantUserId));
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _interviewRepository.GetInterviewSummariesByParticipantAsync(participantUserId, pageNumber, pageSize);

            return await MapSummaryResultAsync<InterviewPublicSummaryDto>(fetchTask, pageNumber, pageSize, $"public participant {participantUserId}");
        }

        public async Task<InterviewDetailDto?> GetInterviewDetailAsync(Guid interviewId, Guid requestingUserId, bool isPrivilegedStaff, bool isRecruiter)
        {
            if (interviewId == Guid.Empty)
                throw new ArgumentException("InterviewId cannot be empty.", nameof(interviewId));

            var projection = await _interviewRepository.GetInterviewDetailProjectionAsync(interviewId);

            if (projection == null)
            {
                return null;
            }

            var isCandidateOwner = projection.JobApplication.CandidateUserId == requestingUserId;
            var isParticipant = projection.Participants.Any(p => p.ParticipantUserId == requestingUserId);
            var isAssignedRecruiter = projection.JobApplication.AssignedRecruiterId.HasValue &&
                projection.JobApplication.AssignedRecruiterId.Value == requestingUserId;

            var hasAccess = isPrivilegedStaff || isCandidateOwner || isParticipant ||
                (isRecruiter && (isAssignedRecruiter || isParticipant));

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You do not have permission to view this interview.");
            }

            var canViewInternal = isPrivilegedStaff || isParticipant || (isRecruiter && (isAssignedRecruiter || isParticipant));

            var dto = _mapper.Map<InterviewDetailDto>(projection);

            dto.Permissions = new InterviewDetailPermissions
            {
                CanViewEvaluations = canViewInternal,
                CanViewInternalNotes = canViewInternal
            };

            // Apply role-based redactions
            if (!canViewInternal)
            {
                dto.Participants.ForEach(p => p.Notes = null);
                dto.SummaryNotes = null;
                dto.ScheduledByUserName = null;
                dto.Evaluations = null;
            }

            return dto;
        }

        #endregion

        #region Private Helper Methods

        private async Task<PagedResult<TDestination>> MapSummaryResultAsync<TDestination>(
            Task<(List<InterviewSummaryProjection> Items, int TotalCount)> fetchTask,
            int pageNumber,
            int pageSize,
            string scenario)
        {
            try
            {
                var (items, totalCount) = await fetchTask;
                var summaryDtos = _mapper.Map<List<TDestination>>(items);
                return PagedResult<TDestination>.Create(summaryDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving interview summaries for {Scenario}", scenario);
                throw;
            }
        }

        private async Task<PagedResult<TDestination>> MapNeedingActionResultAsync<TDestination>(
            Task<(List<InterviewNeedingActionProjection> Items, int TotalCount)> fetchTask,
            int pageNumber,
            int pageSize,
            string scenario)
        {
            try
            {
                var (items, totalCount) = await fetchTask;
                var summaryDtos = _mapper.Map<List<TDestination>>(items);
                return PagedResult<TDestination>.Create(summaryDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving interviews requiring action for {Scenario}", scenario);
                throw;
            }
        }

        #endregion
    }
}