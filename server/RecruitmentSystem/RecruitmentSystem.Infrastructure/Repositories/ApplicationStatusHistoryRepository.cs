using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class ApplicationStatusHistoryRepository : IApplicationStatusHistoryRepository
    {
        private readonly ApplicationDbContext _context;

        public ApplicationStatusHistoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationStatusHistory> CreateAsync(ApplicationStatusHistory statusHistory)
        {
            statusHistory.CreatedAt = DateTime.UtcNow;
            statusHistory.UpdatedAt = DateTime.UtcNow;

            _context.ApplicationStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();
            return statusHistory;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var statusHistory = await _context.ApplicationStatusHistories.FindAsync(id);
            if (statusHistory == null) return false;

            _context.ApplicationStatusHistories.Remove(statusHistory);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ApplicationStatusHistory>> GetByApplicationAsync(Guid jobApplicationId, int? limit = null)
        {
            var query = (IQueryable<ApplicationStatusHistory>)_context.ApplicationStatusHistories
                .AsNoTracking() // Read-only query optimization
                .Include(ash => ash.JobApplication)
                .Include(ash => ash.ChangedByUser)
                .Where(ash => ash.JobApplicationId == jobApplicationId)
                .OrderByDescending(ash => ash.ChangedAt);

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ApplicationStatusHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.ApplicationStatusHistories
                .AsNoTracking() // Read-only query optimization
                .Include(ash => ash.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                .Include(ash => ash.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(ash => ash.ChangedByUser)
                .Where(ash => ash.ChangedAt >= startDate && ash.ChangedAt <= endDate)
                .OrderByDescending(ash => ash.ChangedAt)
                .ToListAsync();
        }
    }
}