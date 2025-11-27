using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Entities.Projections;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class InterviewRepository : IInterviewRepository
    {
        private readonly ApplicationDbContext _context;

        public InterviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Interview> CreateAsync(Interview interview)
        {
            interview.CreatedAt = DateTime.UtcNow;
            interview.UpdatedAt = DateTime.UtcNow;

            _context.Interviews.Add(interview);
            await _context.SaveChangesAsync();
            return interview;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var interview = await _context.Interviews.FindAsync(id);
            if (interview == null) return false;

            _context.Interviews.Remove(interview);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Interviews.AnyAsync(i => i.Id == id);
        }

        public async Task<IEnumerable<Interview>> GetByApplicationAsync(Guid jobApplicationId, bool includeEvaluations = false)
        {
            var query = _context.Interviews
                .Include(i => i.Participants)
                    .ThenInclude(p => p.ParticipantUser)
                .Include(i => i.ScheduledByUser)
                .AsQueryable();

            if (includeEvaluations)
            {
                query = query.Include(i => i.Evaluations)
                    .ThenInclude(e => e.EvaluatorUser);
            }

            return await query
                .Where(i => i.JobApplicationId == jobApplicationId)
                .OrderBy(i => i.RoundNumber)
                .ToListAsync();
        }

        public async Task<(List<InterviewSummaryProjection> Items, int TotalCount)> GetInterviewSummariesByApplicationAsync(Guid jobApplicationId, int pageNumber, int pageSize)
        {
            var query = _context.Interviews
                .AsNoTracking()
                .Where(i => i.JobApplicationId == jobApplicationId)
                .OrderBy(i => i.RoundNumber)
                .ThenBy(i => i.ScheduledDateTime);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Interview>> GetActiveInterviewsByApplicationAsync(Guid jobApplicationId, bool includeEvaluations = false)
        {
            var query = _context.Interviews
                .Include(i => i.Participants)
                    .ThenInclude(p => p.ParticipantUser)
                .Include(i => i.ScheduledByUser)
                .AsQueryable();

            if (includeEvaluations)
            {
                query = query.Include(i => i.Evaluations)
                    .ThenInclude(e => e.EvaluatorUser);
            }

            return await query
                .Where(i => i.JobApplicationId == jobApplicationId && i.IsActive)
                .OrderBy(i => i.RoundNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Interview>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, bool includeBasicDetails = false)
        {
            var query = _context.Interviews.AsQueryable();

            if (includeBasicDetails)
            {
                query = query
                    .Include(i => i.JobApplication)
                        .ThenInclude(ja => ja.JobPosition)
                    .Include(i => i.ScheduledByUser);
            }

            return await query
                .Where(i => i.ScheduledDateTime >= startDate && i.ScheduledDateTime <= endDate)
                .OrderBy(i => i.ScheduledDateTime)
                .ToListAsync();
        }

        public async Task<Interview?> GetByIdAsync(Guid id)
        {
            return await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(i => i.ScheduledByUser)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Interview?> GetByIdWithFullDetailsAsync(Guid id)
        {
            return await _context.Interviews
                .Include(i => i.Participants)
                    .ThenInclude(p => p.ParticipantUser)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(i => i.Evaluations)
                    .ThenInclude(e => e.EvaluatorUser)
                .Include(i => i.ScheduledByUser)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<IEnumerable<Interview>> GetByParticipantAsync(Guid participantUserId, bool includeCandidateInfo = false)
        {
            var query = _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(i => i.ScheduledByUser)
                .AsQueryable();

            if (includeCandidateInfo)
            {
                query = query.Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User);
            }

            return await query
                .Where(i => i.Participants.Any(p => p.ParticipantUserId == participantUserId))
                .OrderBy(i => i.ScheduledDateTime)
                .ToListAsync();
        }

        public async Task<(List<InterviewSummaryProjection> Items, int TotalCount)> GetInterviewSummariesByParticipantAsync(Guid participantUserId, int pageNumber, int pageSize)
        {
            var query = _context.Interviews
                .AsNoTracking()
                .Where(i => i.Participants.Any(p => p.ParticipantUserId == participantUserId))
                .OrderByDescending(i => i.ScheduledDateTime);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Interview>> GetByStatusAsync(InterviewStatus status, bool includeDetails = false)
        {
            var query = _context.Interviews.AsQueryable();

            if (includeDetails)
            {
                query = query
                    .Include(i => i.JobApplication)
                        .ThenInclude(ja => ja.CandidateProfile)
                            .ThenInclude(cp => cp.User)
                    .Include(i => i.JobApplication)
                        .ThenInclude(ja => ja.JobPosition)
                    .Include(i => i.Participants)
                        .ThenInclude(p => p.ParticipantUser)
                    .Include(i => i.ScheduledByUser);
            }

            return await query
                .Where(i => i.Status == status)
                .OrderBy(i => i.ScheduledDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Interview>> GetCompletedInterviewsInDateRangeAsync(DateTime start, DateTime end, bool includeDetails = false)
        {
            var query = _context.Interviews.AsQueryable();

            if (includeDetails)
            {
                query = query
                    .Include(i => i.JobApplication)
                        .ThenInclude(ja => ja.CandidateProfile)
                            .ThenInclude(cp => cp.User)
                    .Include(i => i.JobApplication)
                        .ThenInclude(ja => ja.JobPosition)
                    .Include(i => i.Participants)
                        .ThenInclude(p => p.ParticipantUser);
            }

            return await query
                .Where(i => i.Status == InterviewStatus.Completed && i.UpdatedAt >= start && i.UpdatedAt <= end)
                .OrderBy(i => i.UpdatedAt)
                .ToListAsync();
        }

        public async Task<int> GetInterviewCountForApplicationAsync(Guid applicationId)
        {
            return await _context.Interviews.CountAsync(i => i.JobApplicationId == applicationId);
        }

        public async Task<(List<InterviewSummaryProjection> Items, int TotalCount)> SearchInterviewSummariesAsync(
            InterviewStatus? status = null,
            InterviewType? interviewType = null,
            InterviewMode? mode = null,
            DateTime? scheduledFromDate = null,
            DateTime? scheduledToDate = null,
            Guid? participantUserId = null,
            Guid? jobApplicationId = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            IQueryable<Interview> query = _context.Interviews.AsNoTracking();

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            if (interviewType.HasValue)
                query = query.Where(i => i.InterviewType == interviewType.Value);

            if (mode.HasValue)
                query = query.Where(i => i.Mode == mode.Value);

            if (scheduledFromDate.HasValue)
                query = query.Where(i => i.ScheduledDateTime >= scheduledFromDate.Value);

            if (scheduledToDate.HasValue)
                query = query.Where(i => i.ScheduledDateTime <= scheduledToDate.Value);

            if (jobApplicationId.HasValue)
                query = query.Where(i => i.JobApplicationId == jobApplicationId.Value);

            if (participantUserId.HasValue)
            {
                var participantId = participantUserId.Value;
                query = query.Where(i => i.Participants.Any(p => p.ParticipantUserId == participantId));
            }

            query = query.OrderByDescending(i => i.ScheduledDateTime);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Interview?> GetLatestInterviewForApplicationAsync(Guid applicationId)
        {
            return await _context.Interviews
                .Where(i => i.JobApplicationId == applicationId)
                .OrderByDescending(i => i.ScheduledDateTime)
                .FirstOrDefaultAsync();
        }

        public async Task<(List<InterviewSummaryProjection> Items, int TotalCount)> GetTodayInterviewSummariesAsync(DateTime date, Guid? participantUserId, int pageNumber, int pageSize)
        {
            var query = _context.Interviews
                .AsNoTracking()
                .Where(i => i.ScheduledDateTime.Date == date.Date && i.Status == InterviewStatus.Scheduled);

            if (participantUserId.HasValue)
            {
                var participantId = participantUserId.Value;
                query = query.Where(i => i.Participants.Any(p => p.ParticipantUserId == participantId));
            }

            query = query.OrderBy(i => i.ScheduledDateTime);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<InterviewSummaryProjection> Items, int TotalCount)> GetUpcomingInterviewSummariesForUserAsync(Guid userId, int days, int pageNumber, int pageSize)
        {
            var today = DateTime.UtcNow.Date;
            var upcomingDate = today.AddDays(days);

            var query = _context.Interviews
                .AsNoTracking()
                .Where(i => i.Status == InterviewStatus.Scheduled &&
                            i.ScheduledDateTime.Date >= today &&
                            i.ScheduledDateTime.Date <= upcomingDate &&
                            (i.Participants.Any(p => p.ParticipantUserId == userId) ||
                             i.JobApplication.CandidateProfile.UserId == userId))
                .OrderBy(i => i.ScheduledDateTime);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Interview> UpdateAsync(Interview interview)
        {
            interview.UpdatedAt = DateTime.UtcNow;

            _context.Interviews.Update(interview);
            await _context.SaveChangesAsync();
            return interview;
        }

        public async Task<InterviewStatus?> GetInterviewStatusAsync(Guid interviewId)
        {
            var interview = await _context.Interviews
                .Where(i => i.Id == interviewId)
                .Select(i => new { i.Status })
                .FirstOrDefaultAsync();

            return interview?.Status;
        }

        private static IQueryable<InterviewSummaryProjection> ProjectToSummary(IQueryable<Interview> query)
        {
            return query.Select(i => new InterviewSummaryProjection
            {
                Id = i.Id,
                JobApplicationId = i.JobApplicationId,
                Title = i.Title,
                InterviewType = i.InterviewType,
                RoundNumber = i.RoundNumber,
                Status = i.Status,
                ScheduledDateTime = i.ScheduledDateTime,
                Mode = i.Mode,
                Outcome = i.Outcome,
                ParticipantCount = i.Participants.Count(),
                EvaluationCount = i.Evaluations.Count(),
                AverageRating = i.Evaluations
                    .Where(e => e.OverallRating.HasValue)
                    .Average(e => (double?)e.OverallRating)
            });
        }
    }
}