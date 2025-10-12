using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
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

        public async Task<IEnumerable<Interview>> GetByApplicationAsync(Guid jobApplicationId)
        {
            return await _context.Interviews
                .Where(i => i.JobApplicationId == jobApplicationId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Interview>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Interviews
                .Where(i => i.ScheduledDateTime >= startDate && i.ScheduledDateTime <= endDate)
                .ToListAsync();
        }

        public async Task<Interview?> GetByIdAsync(Guid id)
        {
            return await _context.Interviews.FindAsync(id);
        }

        public async Task<Interview?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.Interviews
                .Include(i => i.Participants)
                .Include(i => i.JobApplication)
                .Include(i => i.Evaluations)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<IEnumerable<Interview>> GetByParticipantAsync(Guid participantUserId)
        {
            return await _context.Interviews
                .Where(i => i.Participants.Any(p => p.ParticipantUserId == participantUserId))
                .ToListAsync();
        }

        public async Task<IEnumerable<Interview>> GetByStatusAsync(InterviewStatus status)
        {
            return await _context.Interviews
                .Where(i => i.Status == status)
                .ToListAsync();
        }

        public async Task<int> GetInterviewCountForApplicationAsync(Guid applicationId)
        {
            return await _context.Interviews.CountAsync(i => i.JobApplicationId == applicationId);
        }

        public async Task<IEnumerable<Interview>> GetInterviewsWithFiltersAsync(InterviewStatus? status = null, InterviewType? interviewType = null, InterviewMode? mode = null, DateTime? scheduledFromDate = null, DateTime? scheduledToDate = null)
        {
            var query = _context.Interviews.AsQueryable();

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

            return await query.ToListAsync();
        }

        public async Task<Interview?> GetLatestInterviewForApplicationAsync(Guid applicationId)
        {
            return await _context.Interviews
                .Where(i => i.JobApplicationId == applicationId)
                .OrderByDescending(i => i.ScheduledDateTime)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Interview>> GetScheduledInterviewsAsync(DateTime date)
        {
            return await _context.Interviews
                .Where(i => i.ScheduledDateTime.Date == date.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Interview>> GetUpcomingInterviewsForUserAsync(Guid userId, int days = 7)
        {
            var today = DateTime.UtcNow.Date;
            var upcomingDate = today.AddDays(days);

            return await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(i => i.Participants)
                    .ThenInclude(p => p.ParticipantUser)
                .Include(i => i.ScheduledByUser)
                .Where(i => i.Status == InterviewStatus.Scheduled &&
                            i.ScheduledDateTime.Date >= today &&
                            i.ScheduledDateTime.Date <= upcomingDate &&
                            (i.Participants.Any(p => p.ParticipantUserId == userId) ||  // User is staff participant
                             i.JobApplication.CandidateProfile.UserId == userId))      // User is candidate
                .OrderBy(i => i.ScheduledDateTime)
                .ToListAsync();
        }

        public async Task<Interview> UpdateAsync(Interview interview)
        {
            interview.UpdatedAt = DateTime.UtcNow;

            _context.Interviews.Update(interview);
            await _context.SaveChangesAsync();
            return interview;
        }

        public async Task<IEnumerable<Interview>> GetUpcomingInterviewsForCandidateAsync(Guid candidateUserId, int days = 7)
        {
            var today = DateTime.UtcNow.Date;
            var upcomingDate = today.AddDays(days);

            return await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(i => i.Participants)
                    .ThenInclude(p => p.ParticipantUser)
                .Include(i => i.ScheduledByUser)
                .Where(i => i.JobApplication.CandidateProfile.UserId == candidateUserId &&
                            i.ScheduledDateTime.Date >= today &&
                            i.ScheduledDateTime.Date <= upcomingDate &&
                            i.Status == InterviewStatus.Scheduled)
                .OrderBy(i => i.ScheduledDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Interview>> GetUpcomingInterviewsForStaffAsync(Guid staffUserId, int days = 7)
        {
            var today = DateTime.UtcNow.Date;
            var upcomingDate = today.AddDays(days);

            return await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)  // Load candidate's user info (name, email)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(i => i.Participants)
                    .ThenInclude(p => p.ParticipantUser)
                .Where(i => i.Participants.Any(p => p.ParticipantUserId == staffUserId) &&
                            i.ScheduledDateTime.Date >= today &&
                            i.ScheduledDateTime.Date <= upcomingDate &&
                            i.Status == InterviewStatus.Scheduled)
                .OrderBy(i => i.ScheduledDateTime)
                .ToListAsync();
        }
    }
}