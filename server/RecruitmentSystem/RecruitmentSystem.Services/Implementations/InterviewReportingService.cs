using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
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

        public async Task<PagedResult<InterviewSummaryDto>> SearchInterviewsAsync(InterviewSearchDto searchDto)
        {
            try
            {
                var pageNumber = searchDto.PageNumber ?? 1;
                var pageSize = searchDto.PageSize ?? 20;

                if (pageNumber < 1)
                    throw new ArgumentException("Page number must be greater than 0.", nameof(searchDto.PageNumber));
                if (pageSize < 1 || pageSize > 100)
                    throw new ArgumentException("Page size must be between 1 and 100.", nameof(searchDto.PageSize));

                var pagedInterviews = await _interviewRepository.SearchInterviewsAsync(
                    status: searchDto.Status,
                    interviewType: searchDto.InterviewType,
                    mode: searchDto.Mode,
                    scheduledFromDate: searchDto.ScheduledFromDate,
                    scheduledToDate: searchDto.ScheduledToDate,
                    participantUserId: searchDto.ParticipantUserId,
                    jobApplicationId: searchDto.JobApplicationId,
                    pageNumber: pageNumber,
                    pageSize: pageSize,
                    includeDetails: true);

                var summaryDtos = await EnrichInterviewSummariesAsync(pagedInterviews.Items);

                return PagedResult<InterviewSummaryDto>.Create(
                    summaryDtos,
                    pagedInterviews.TotalCount,
                    pageNumber,
                    pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching interviews with criteria: {@SearchDto}", searchDto);
                throw;
            }
        }

        public async Task<PagedResult<Interview>> GetUpcomingInterviewsForUserAsync(Guid userId, int days = 7, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (userId == Guid.Empty)
                    throw new ArgumentException("UserId cannot be empty.", nameof(userId));
                if (days < 1 || days > 90)
                    throw new ArgumentException("Days must be between 1 and 90.", nameof(days));
                if (pageNumber < 1)
                    throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
                if (pageSize < 1 || pageSize > 100)
                    throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

                var interviews = await _interviewRepository.GetUpcomingInterviewsForUserAsync(userId, days);
                var interviewsList = interviews.ToList();

                var totalCount = interviewsList.Count;
                var paginatedInterviews = interviewsList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return PagedResult<Interview>.Create(paginatedInterviews, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming interviews for user {UserId}", userId);
                throw;
            }
        }

        public async Task<PagedResult<Interview>> GetTodayInterviewsAsync(Guid? participantUserId = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1)
                    throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
                if (pageSize < 1 || pageSize > 100)
                    throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

                var today = DateTime.UtcNow.Date;

                var todayInterviews = await _interviewRepository.GetScheduledInterviewsAsync(today);
                var interviewsList = todayInterviews.ToList();

                if (participantUserId.HasValue)
                {
                    var participantInterviewIds = await _participantRepository.GetInterviewIdsByUserAsync(participantUserId.Value);
                    interviewsList = interviewsList.Where(i => participantInterviewIds.Contains(i.Id)).ToList();
                }

                var totalCount = interviewsList.Count;
                var paginatedInterviews = interviewsList
                    .OrderBy(i => i.ScheduledDateTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return PagedResult<Interview>.Create(paginatedInterviews, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's interviews" +
                    (participantUserId.HasValue ? " for participant {ParticipantUserId}" : ""),
                    participantUserId);
                throw;
            }
        }

        public async Task<PagedResult<Interview>> GetInterviewsNeedingActionAsync(Guid? userId = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1)
                    throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
                if (pageSize < 1 || pageSize > 100)
                    throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

                var needingAction = new List<Interview>();

                var completedInterviews = await _interviewRepository.GetByStatusAsync(InterviewStatus.Completed, includeDetails: true);
                foreach (var interview in completedInterviews)
                {
                    var daysSinceCompletion = (DateTime.UtcNow - interview.ScheduledDateTime).TotalDays;
                    if (daysSinceCompletion > 7)
                        continue;

                    var participants = await _participantRepository.GetByInterviewAsync(interview.Id);
                    var evaluations = await _evaluationRepository.GetByInterviewAsync(interview.Id);

                    var participantIds = participants.Select(p => p.ParticipantUserId).ToHashSet();
                    var evaluatorIds = evaluations.Select(e => e.EvaluatorUserId).ToHashSet();

                    if (!participantIds.IsSubsetOf(evaluatorIds))
                    {
                        if (!userId.HasValue || participantIds.Contains(userId.Value))
                        {
                            needingAction.Add(interview);
                        }
                    }
                }

                var scheduledInterviews = await _interviewRepository.GetByStatusAsync(InterviewStatus.Scheduled, includeDetails: true);
                var overdueInterviews = scheduledInterviews
                    .Where(i => i.ScheduledDateTime.AddMinutes(i.DurationMinutes) < DateTime.UtcNow)
                    .ToList();

                needingAction.AddRange(overdueInterviews);

                var orderedResults = needingAction
                    .OrderBy(i => i.Status == InterviewStatus.Scheduled &&
                                 i.ScheduledDateTime.AddMinutes(i.DurationMinutes) < DateTime.UtcNow ? 0 : 1)
                    .ThenBy(i => i.ScheduledDateTime)
                    .Distinct()
                    .ToList();

                var totalCount = orderedResults.Count;
                var paginatedResults = orderedResults
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return PagedResult<Interview>.Create(paginatedResults, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interviews needing action" +
                    (userId.HasValue ? " for user {UserId}" : ""),
                    userId);
                throw;
            }
        }

        #endregion

        #region Application-specific Queries

        public async Task<PagedResult<Interview>> GetInterviewsByApplicationAsync(Guid jobApplicationId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (jobApplicationId == Guid.Empty)
                    throw new ArgumentException("JobApplicationId cannot be empty.", nameof(jobApplicationId));
                if (pageNumber < 1)
                    throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
                if (pageSize < 1 || pageSize > 100)
                    throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

                var interviews = await _interviewRepository.GetByApplicationAsync(jobApplicationId, includeEvaluations: true);
                var interviewsList = interviews.ToList();

                var totalCount = interviewsList.Count;
                var paginatedInterviews = interviewsList
                    .OrderBy(i => i.RoundNumber)
                    .ThenBy(i => i.ScheduledDateTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return PagedResult<Interview>.Create(paginatedInterviews, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interviews for job application {JobApplicationId}", jobApplicationId);
                throw;
            }
        }

        public async Task<PagedResult<Interview>> GetInterviewsByParticipantAsync(Guid participantUserId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (participantUserId == Guid.Empty)
                    throw new ArgumentException("ParticipantUserId cannot be empty.", nameof(participantUserId));
                if (pageNumber < 1)
                    throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
                if (pageSize < 1 || pageSize > 100)
                    throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

                var interviews = await _interviewRepository.GetByParticipantAsync(participantUserId, includeCandidateInfo: true);
                var interviewsList = interviews.ToList();

                var totalCount = interviewsList.Count;
                var paginatedInterviews = interviewsList
                    .OrderByDescending(i => i.ScheduledDateTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return PagedResult<Interview>.Create(paginatedInterviews, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interviews for participant {ParticipantUserId}", participantUserId);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task<List<InterviewSummaryDto>> EnrichInterviewSummariesAsync(IEnumerable<Interview> interviews)
        {
            var summaryDtos = new List<InterviewSummaryDto>();

            foreach (var interview in interviews)
            {
                var summary = _mapper.Map<InterviewSummaryDto>(interview);

                summary.ParticipantCount = await _participantRepository.GetParticipantCountForInterviewAsync(interview.Id);
                summary.EvaluationCount = (await _evaluationRepository.GetByInterviewAsync(interview.Id)).Count();

                if (summary.EvaluationCount > 0)
                {
                    var ratings = await _evaluationRepository.GetOverallRatingsByInterviewAsync(interview.Id);
                    var validRatings = ratings.Where(r => r.HasValue).Select(r => r!.Value);
                    summary.AverageRating = validRatings.Any() ? validRatings.Average() : null;
                }

                summaryDtos.Add(summary);
            }

            return summaryDtos;
        }

        #endregion
    }
}