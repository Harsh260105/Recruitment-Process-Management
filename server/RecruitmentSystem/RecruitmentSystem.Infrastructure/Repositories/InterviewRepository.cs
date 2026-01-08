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
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
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
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                .Where(i =>
                    i.Participants.Any(p => p.ParticipantUserId == participantUserId) ||
                    (i.JobApplication.CandidateProfile != null &&
                     i.JobApplication.CandidateProfile.UserId == participantUserId))
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
            Guid? userId,
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
            IQueryable<Interview> query = _context.Interviews
                .AsNoTracking()
                .Include(i => i.Participants)
                .Include(i => i.Evaluations)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User);

            if (userId.HasValue)
            {
                query = query.Where(i =>
                    i.Participants.Any(p => p.ParticipantUserId == userId) ||
                    (i.JobApplication.AssignedRecruiterId != null &&
                     i.JobApplication.AssignedRecruiterId == userId));
            }

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
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
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
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
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

        public async Task<(List<InterviewNeedingActionProjection> Items, int TotalCount)> GetInterviewsNeedingActionProjectionAsync(Guid? userId, bool isPrivilegedStaff, bool isRecruiter, int pageNumber = 1, int pageSize = 20)
        {
            var now = DateTime.UtcNow;

            var query = _context.Interviews
                .AsNoTracking()
                .Where(i =>
                    (i.Status == InterviewStatus.Completed && i.ScheduledDateTime > now.AddDays(-7)) || // Completed within last 7 days
                    (i.Status == InterviewStatus.Scheduled && i.ScheduledDateTime.AddMinutes(i.DurationMinutes) < now) // Overdue scheduled
                )
                .Select(i => new InterviewNeedingActionProjection
                {
                    Id = i.Id,
                    JobApplicationId = i.JobApplicationId,
                    Title = i.Title,
                    InterviewType = i.InterviewType,
                    RoundNumber = i.RoundNumber,
                    Status = i.Status,
                    ScheduledDateTime = i.ScheduledDateTime,
                    DurationMinutes = i.DurationMinutes,
                    Mode = i.Mode,
                    Outcome = i.Outcome,
                    CandidateUserId = i.JobApplication.CandidateProfile.UserId,
                    CandidateName = i.JobApplication.CandidateProfile.User.FirstName + " " + i.JobApplication.CandidateProfile.User.LastName,
                    ParticipantUserIds = i.Participants.Select(p => p.ParticipantUserId).ToList(),
                    EvaluationCount = i.Evaluations.Count()
                });

            // Apply business logic filtering
            query = query.Where(p =>
                (p.Status == InterviewStatus.Completed && p.EvaluationCount < p.ParticipantUserIds.Count) || // Missing evaluations
                (p.Status == InterviewStatus.Scheduled) // Overdue (already filtered above)
            );

            if (isRecruiter && userId.HasValue)
            {
                query = query.Where(p =>
                    p.ParticipantUserIds.Contains(userId.Value) ||
                    _context.JobApplications.Any(ja => ja.Id == p.JobApplicationId && ja.AssignedRecruiterId == userId.Value)
                );
            }

            var totalCount = await query.CountAsync();

            query = query
                .OrderBy(p => p.Status == InterviewStatus.Scheduled ? 0 : 1)
                .ThenBy(p => p.ScheduledDateTime);

            // Paginate
            var results = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (results, totalCount);
        }

        public async Task<InterviewDetailProjection?> GetInterviewDetailProjectionAsync(Guid interviewId)
        {
            return await ProjectToDetail(_context.Interviews
                .AsNoTracking()
                .Where(i => i.Id == interviewId))
                .FirstOrDefaultAsync();
        }

        private static IQueryable<InterviewDetailProjection> ProjectToDetail(IQueryable<Interview> query)
        {
            return query.Select(i => new InterviewDetailProjection
            {
                Id = i.Id,
                JobApplicationId = i.JobApplicationId,
                Title = i.Title,
                InterviewType = i.InterviewType,
                RoundNumber = i.RoundNumber,
                Status = i.Status,
                ScheduledDateTime = i.ScheduledDateTime,
                DurationMinutes = i.DurationMinutes,
                Mode = i.Mode,
                MeetingDetails = i.MeetingDetails,
                Instructions = i.Instructions,
                ScheduledByUserId = i.ScheduledByUserId,
                ScheduledByUserName = i.ScheduledByUser.FirstName + " " + i.ScheduledByUser.LastName,
                Outcome = i.Outcome,
                SummaryNotes = i.SummaryNotes,
                JobApplication = new InterviewDetailJobApplicationProjection
                {
                    JobApplicationId = i.JobApplicationId,
                    CandidateProfileId = i.JobApplication.CandidateProfileId,
                    CandidateUserId = i.JobApplication.CandidateProfile.UserId,
                    CandidateFirstName = i.JobApplication.CandidateProfile.User.FirstName,
                    CandidateLastName = i.JobApplication.CandidateProfile.User.LastName,
                    CandidateEmail = i.JobApplication.CandidateProfile.User.Email,
                    AssignedRecruiterId = i.JobApplication.AssignedRecruiterId,
                    AssignedRecruiterName = i.JobApplication.AssignedRecruiter != null
                        ? i.JobApplication.AssignedRecruiter.FirstName + " " + i.JobApplication.AssignedRecruiter.LastName
                        : null,
                    JobPositionId = i.JobApplication.JobPositionId,
                    JobPositionTitle = i.JobApplication.JobPosition.Title,
                    JobPositionDepartment = i.JobApplication.JobPosition.Department,
                    JobPositionLocation = i.JobApplication.JobPosition.Location
                },
                Participants = i.Participants.Select(p => new InterviewDetailParticipantProjection
                {
                    Id = p.Id,
                    ParticipantUserId = p.ParticipantUserId,
                    ParticipantName = p.ParticipantUser.FirstName + " " + p.ParticipantUser.LastName,
                    ParticipantEmail = p.ParticipantUser.Email,
                    Role = p.Role,
                    IsLead = p.IsLead,
                    CreatedAt = p.CreatedAt,
                    Notes = p.Notes
                }).ToList(),
                Evaluations = i.Evaluations.Select(e => new InterviewDetailEvaluationProjection
                {
                    Id = e.Id,
                    EvaluatorUserId = e.EvaluatorUserId,
                    EvaluatorName = e.EvaluatorUser.FirstName + " " + e.EvaluatorUser.LastName,
                    EvaluatorEmail = e.EvaluatorUser.Email,
                    OverallRating = e.OverallRating,
                    Strengths = e.Strengths,
                    Concerns = e.Concerns,
                    AdditionalComments = e.AdditionalComments,
                    Recommendation = e.Recommendation,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                }).ToList()
            });
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
                    .Average(e => (double?)e.OverallRating),
                CandidateName = i.JobApplication.CandidateProfile.User.FirstName + " " + i.JobApplication.CandidateProfile.User.LastName
            });
        }
    }
}