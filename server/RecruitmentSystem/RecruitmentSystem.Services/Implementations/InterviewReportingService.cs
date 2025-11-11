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
    /// <summary>
    /// Interview reporting service implementation
    /// Handles analytics, reporting, and search capabilities for interviews
    /// </summary>
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

        /// <summary>
        /// Gets interview status distribution with optional date filtering
        /// Groups interviews by status and counts occurrences
        /// </summary>
        public async Task<Dictionary<InterviewStatus, int>> GetInterviewStatusDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                // Set default date range if not provided (last 30 days)
                var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var endDate = toDate ?? DateTime.UtcNow;

                if (startDate > endDate)
                    throw new ArgumentException("From date cannot be after to date.", nameof(fromDate));

                // Get interviews in date range
                var interviews = await _interviewRepository.GetByDateRangeAsync(startDate, endDate);

                // Group by status and count
                var distribution = interviews
                    .GroupBy(i => i.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Ensure all status values are represented (0 count if none)
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

        /// <summary>
        /// Gets interview type distribution with optional date filtering
        /// Groups interviews by type and counts occurrences
        /// </summary>
        public async Task<Dictionary<InterviewType, int>> GetInterviewTypeDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                // Set default date range if not provided (last 30 days)
                var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var endDate = toDate ?? DateTime.UtcNow;

                if (startDate > endDate)
                    throw new ArgumentException("From date cannot be after to date.", nameof(fromDate));

                // Get interviews in date range
                var interviews = await _interviewRepository.GetByDateRangeAsync(startDate, endDate);

                // Group by type and count
                var distribution = interviews
                    .GroupBy(i => i.InterviewType)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Ensure all type values are represented (0 count if none)
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

        /// <summary>
        /// Gets comprehensive interview analytics
        /// Combines multiple statistics into single response with derived metrics
        /// </summary>
        public async Task<InterviewAnalyticsDto> GetInterviewAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                // Set default date range if not provided (last 30 days)
                var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var endDate = toDate ?? DateTime.UtcNow;

                if (startDate > endDate)
                    throw new ArgumentException("From date cannot be after to date.", nameof(fromDate));

                // Get interviews in date range with basic details
                var interviews = await _interviewRepository.GetByDateRangeAsync(startDate, endDate, includeBasicDetails: true);
                var interviewsList = interviews.ToList();

                // Calculate distributions
                var statusDistribution = interviewsList
                    .GroupBy(i => i.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                var typeDistribution = interviewsList
                    .GroupBy(i => i.InterviewType.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate metrics
                var totalInterviews = interviewsList.Count;
                var upcomingInterviews = interviewsList.Count(i => i.Status == InterviewStatus.Scheduled && i.ScheduledDateTime > DateTime.UtcNow);
                var completedInterviews = interviewsList.Count(i => i.Status == InterviewStatus.Completed);
                var cancelledInterviews = interviewsList.Count(i => i.Status == InterviewStatus.Cancelled);

                // Calculate average duration for completed interviews
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

        /// <summary>
        /// Searches interviews with advanced filtering and pagination
        /// Optimized with database-level pagination for better performance
        /// SERVICE handles mapping: Interview entities -> InterviewSummaryDto (via AutoMapper)
        /// CONTROLLER passes search criteria DTO to service
        /// </summary>
        /// <param name="searchDto">Search criteria from controller</param>
        /// <returns>Paginated results with mapped DTOs</returns>
        public async Task<PagedResult<InterviewSummaryDto>> SearchInterviewsAsync(InterviewSearchDto searchDto)
        {
            try
            {
                // Validate and set defaults
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

                // Enrich only the paginated results with computed fields (not thousands of records)
                var summaryDtos = await EnrichInterviewSummariesAsync(pagedInterviews.Items);

                // Convert from Core PagedResult<Interview> to Shared PagedResult<InterviewSummaryDto>
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

        /// <summary>
        /// Gets upcoming interviews for a user (candidate or staff)
        /// Uses existing repository method for efficiency
        /// </summary>
        /// <param name="userId">User ID (from controller context)</param>
        /// <param name="days">Number of days to look ahead (default: 7)</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Paginated collection of upcoming interview entities</returns>
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

                // Use existing repository method
                var interviews = await _interviewRepository.GetUpcomingInterviewsForUserAsync(userId, days);
                var interviewsList = interviews.ToList();

                // Apply pagination
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

        /// <summary>
        /// Gets today's interviews with optional participant filtering
        /// Filters scheduled interviews to today's date
        /// </summary>
        public async Task<PagedResult<Interview>> GetTodayInterviewsAsync(Guid? participantUserId = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1)
                    throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
                if (pageSize < 1 || pageSize > 100)
                    throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

                var today = DateTime.UtcNow.Date;

                // Get all scheduled interviews for today
                var todayInterviews = await _interviewRepository.GetScheduledInterviewsAsync(today);
                var interviewsList = todayInterviews.ToList();

                // Filter by participant if specified
                if (participantUserId.HasValue)
                {
                    var participantInterviewIds = await _participantRepository.GetInterviewIdsByUserAsync(participantUserId.Value);
                    interviewsList = interviewsList.Where(i => participantInterviewIds.Contains(i.Id)).ToList();
                }

                // Apply pagination and ordering
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

        /// <summary>
        /// Gets interviews requiring action (evaluation, scheduling, etc.)
        /// Identifies interviews in states needing attention based on business rules
        /// </summary>
        public async Task<PagedResult<Interview>> GetInterviewsNeedingActionAsync(Guid? userId = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1)
                    throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
                if (pageSize < 1 || pageSize > 100)
                    throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

                var needingAction = new List<Interview>();

                // 1. Completed interviews without evaluations (within evaluation window)
                var completedInterviews = await _interviewRepository.GetByStatusAsync(InterviewStatus.Completed, includeDetails: true);
                foreach (var interview in completedInterviews)
                {
                    // Check if evaluation window is still open (7 days after completion)
                    var daysSinceCompletion = (DateTime.UtcNow - interview.ScheduledDateTime).TotalDays;
                    if (daysSinceCompletion > 7)
                        continue;

                    // Check if all participants have evaluated
                    var participants = await _participantRepository.GetByInterviewAsync(interview.Id);
                    var evaluations = await _evaluationRepository.GetByInterviewAsync(interview.Id);

                    var participantIds = participants.Select(p => p.ParticipantUserId).ToHashSet();
                    var evaluatorIds = evaluations.Select(e => e.EvaluatorUserId).ToHashSet();

                    if (!participantIds.IsSubsetOf(evaluatorIds))
                    {
                        // Filter by user if specified
                        if (!userId.HasValue || participantIds.Contains(userId.Value))
                        {
                            needingAction.Add(interview);
                        }
                    }
                }

                // 2. Scheduled interviews that are overdue (past scheduled time but not marked completed/cancelled)
                var scheduledInterviews = await _interviewRepository.GetByStatusAsync(InterviewStatus.Scheduled, includeDetails: true);
                var overdueInterviews = scheduledInterviews
                    .Where(i => i.ScheduledDateTime.AddMinutes(i.DurationMinutes) < DateTime.UtcNow)
                    .ToList();

                needingAction.AddRange(overdueInterviews);

                // 3. Interviews with conflicting schedules (same participant, overlapping times)
                // This would require more complex logic - for now, just return what we have

                // Order by priority: overdue first, then evaluation deadlines
                var orderedResults = needingAction
                    .OrderBy(i => i.Status == InterviewStatus.Scheduled &&
                                 i.ScheduledDateTime.AddMinutes(i.DurationMinutes) < DateTime.UtcNow ? 0 : 1)
                    .ThenBy(i => i.ScheduledDateTime)
                    .Distinct()
                    .ToList();

                // Apply pagination
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

        /// <summary>
        /// Gets all interviews for a specific job application
        /// Uses existing repository method for efficiency
        /// </summary>
        /// <param name="jobApplicationId">Job application ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Paginated collection of interview entities ordered by round and date</returns>
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

                // Use existing repository method
                var interviews = await _interviewRepository.GetByApplicationAsync(jobApplicationId, includeEvaluations: true);
                var interviewsList = interviews.ToList();

                // Apply pagination and ordering
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

        /// <summary>
        /// Gets interviews where user was a participant with pagination
        /// Uses existing repository method with additional ordering
        /// </summary>
        /// <param name="participantUserId">Participant user ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Paginated collection of interview entities</returns>
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

                // Use existing repository method
                var interviews = await _interviewRepository.GetByParticipantAsync(participantUserId, includeCandidateInfo: true);
                var interviewsList = interviews.ToList();

                // Apply pagination and ordering
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

        /// <summary>
        /// Enriches a collection of interviews with computed fields for summary DTOs
        /// Optimized to process only paginated results instead of all records
        /// </summary>
        /// <param name="interviews">Collection of interview entities to enrich</param>
        /// <returns>Collection of enriched InterviewSummaryDto objects</returns>
        private async Task<List<InterviewSummaryDto>> EnrichInterviewSummariesAsync(IEnumerable<Interview> interviews)
        {
            var summaryDtos = new List<InterviewSummaryDto>();

            foreach (var interview in interviews)
            {
                var summary = _mapper.Map<InterviewSummaryDto>(interview);

                // Add computed fields
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