using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class JobApplicationRepository : IJobApplicationRepository
    {

        private readonly ApplicationDbContext _context;

        public JobApplicationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<JobApplication> CreateAsync(JobApplication application)
        {
            // AppliedDate is set by the service layer
            application.CreatedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();
            return application;
        }

        public async Task<JobApplication> CompleteTestWithScoreAsync(Guid applicationId, int testScore, ApplicationStatus newStatus, Guid changedByUserId, string? comments = null)
        {
            var application = await _context.JobApplications
                .SingleAsync(a => a.Id == applicationId);

            var changedByUser = await _context.Users.FindAsync(changedByUserId);
            if (changedByUser == null)
                throw new ArgumentException($"User with ID {changedByUserId} not found", nameof(changedByUserId));

            var oldStatus = application.Status;

            // Update test score and completion time
            application.TestScore = testScore;
            application.TestCompletedAt = DateTime.UtcNow;

            // Update status
            application.Status = newStatus;
            application.UpdatedAt = DateTime.UtcNow;

            var statusHistory = new ApplicationStatusHistory
            {
                JobApplicationId = applicationId,
                JobApplication = application,
                FromStatus = oldStatus,
                ToStatus = newStatus,
                ChangedByUserId = changedByUserId,
                ChangedByUser = changedByUser,
                ChangedAt = DateTime.UtcNow,
                Comments = comments,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ApplicationStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();
            return application;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var job = await _context.JobApplications.FindAsync(id);
            if (job == null) return false;

            _context.JobApplications.Remove(job);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.JobApplications.AnyAsync(j => j.Id == id);
        }

        public async Task<int> GetApplicationCountByJobAsync(Guid jobPositionId)
        {
            return await _context.JobApplications.CountAsync(a => a.JobPositionId == jobPositionId);
        }

        public async Task<int> GetApplicationCountByStatusAsync(ApplicationStatus status)
        {
            return await _context.JobApplications.CountAsync(a => a.Status == status);
        }

        public async Task<IEnumerable<JobApplication>> GetApplicationsWithFiltersAsync(ApplicationStatus? status = null, Guid? jobPositionId = null, Guid? candidateProfileId = null, Guid? assignedRecruiterId = null, DateTime? appliedFromDate = null, DateTime? appliedToDate = null, string? searchTerm = null)
        {
            IQueryable<JobApplication> query = _context.JobApplications
                .AsNoTracking()
                .Include(a => a.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate name mapping
                .Include(a => a.JobPosition)
                .Include(a => a.AssignedRecruiter);

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            if (jobPositionId.HasValue)
                query = query.Where(a => a.JobPositionId == jobPositionId.Value);

            if (candidateProfileId.HasValue)
                query = query.Where(a => a.CandidateProfileId == candidateProfileId.Value);

            if (assignedRecruiterId.HasValue)
                query = query.Where(a => a.AssignedRecruiterId == assignedRecruiterId.Value);

            if (appliedFromDate.HasValue)
                query = query.Where(a => a.AppliedDate >= appliedFromDate.Value);

            if (appliedToDate.HasValue)
                query = query.Where(a => a.AppliedDate <= appliedToDate.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchTermLower = searchTerm.ToLower();
                query = query.Where(a =>
                    (a.CandidateProfile.User != null &&
                     (a.CandidateProfile.User.FirstName + " " + a.CandidateProfile.User.LastName).ToLower().Contains(searchTermLower)) ||
                    (a.JobPosition != null && a.JobPosition.Title.ToLower().Contains(searchTermLower)) ||
                    (a.AssignedRecruiter != null &&
                     (a.AssignedRecruiter.FirstName + " " + a.AssignedRecruiter.LastName).ToLower().Contains(searchTermLower)) ||
                    (a.CoverLetter != null && a.CoverLetter.ToLower().Contains(searchTermLower)));
            }

            return await query
                .OrderByDescending(a => a.AppliedDate) // Most recent first
                .ToListAsync();
        }

        public async Task<IEnumerable<JobApplication>> GetByCandidateIdAsync(Guid candidateProfileId)
        {
            return await _context.JobApplications
                .AsNoTracking()
                .Include(j => j.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate name mapping
                .Include(j => j.JobPosition)
                .Include(j => j.AssignedRecruiter) // Include for recruiter name mapping
                .Where(j => j.CandidateProfileId == candidateProfileId)
                .OrderByDescending(j => j.AppliedDate) // Most recent first
                .ToListAsync();
        }

        public async Task<JobApplication?> GetByIdAsync(Guid id)
        {
            return await _context.JobApplications
                .Include(j => j.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate name mapping
                .Include(j => j.JobPosition)
                .Include(j => j.AssignedRecruiter) // Include for recruiter name mapping
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<JobApplication?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.JobApplications
                .AsNoTracking()
                .Include(j => j.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate details
                .Include(j => j.JobPosition)
                .Include(j => j.AssignedRecruiter)
                .Include(j => j.StatusHistory.OrderByDescending(sh => sh.ChangedAt).Take(5)) // Limit to recent history
                    .ThenInclude(sh => sh.ChangedByUser)
                .Include(j => j.JobOffer)
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<ApplicationStatus?> GetStatusByIdAsync(Guid id)
        {
            return await _context.JobApplications
                .Where(a => a.Id == id)
                .Select(a => (ApplicationStatus?)a.Status)
                .FirstOrDefaultAsync();
        }

        public async Task<JobApplication?> GetByJobAndCandidateAsync(Guid jobPositionId, Guid candidateProfileId)
        {
            return await _context.JobApplications
                .Include(j => j.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate name mapping
                .Include(j => j.JobPosition)
                .Include(j => j.AssignedRecruiter) // Include for recruiter name mapping
                .Where(j => j.JobPositionId == jobPositionId && j.CandidateProfileId == candidateProfileId)
                .OrderByDescending(j => j.UpdatedAt)
                .ThenByDescending(j => j.AppliedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<(List<JobApplication> Items, int TotalCount)> GetByJobPositionIdForUserAsync(
            Guid jobPositionId, Guid userId, List<string> userRoles, int pageNumber, int pageSize)
        {
            var query = _context.JobApplications
                .AsNoTracking()
                .Include(j => j.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate name mapping
                .Include(j => j.JobPosition) // Include JobPosition for job title mapping
                .Include(j => j.AssignedRecruiter)
                .Where(j => j.JobPositionId == jobPositionId);

            var hasAdminRoles = userRoles.Any(r => r == "SuperAdmin" || r == "Admin" || r == "HR");

            if (!hasAdminRoles)
            {
                if (userRoles.Contains("Recruiter"))
                {
                    // Recruiters only see applications assigned to them
                    query = query.Where(j => j.AssignedRecruiterId == userId);
                }
                else
                {
                    // No access - return empty result
                    return (new List<JobApplication>(), 0);
                }
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(j => j.AppliedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<JobApplication> Items, int TotalCount)> GetByCandidateIdPagedAsync(Guid candidateProfileId, int pageNumber, int pageSize)
        {
            var query = _context.JobApplications
                .AsNoTracking()
                .Include(j => j.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate name mapping
                .Include(j => j.JobPosition)
                .Include(j => j.AssignedRecruiter) // Include for recruiter name mapping
                .Where(j => j.CandidateProfileId == candidateProfileId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(j => j.AppliedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<JobApplication> Items, int TotalCount)> GetByStatusAsync(ApplicationStatus status, int pageNumber, int pageSize)
        {
            var query = _context.JobApplications
                .AsNoTracking()
                .Include(j => j.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate name mapping
                .Include(j => j.JobPosition)
                .Include(j => j.AssignedRecruiter) // Include for recruiter name mapping
                .Where(j => j.Status == status);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(j => j.AppliedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<JobApplication>> GetByRecruiterAsync(Guid recruiterId)
        {
            return await _context.JobApplications
                .AsNoTracking() // Read-only query optimization
                .Include(j => j.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate name mapping
                .Include(j => j.JobPosition)
                .Include(j => j.AssignedRecruiter) // Include for recruiter name mapping
                .Where(j => j.AssignedRecruiterId == recruiterId)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobApplication>> GetRecentApplicationsAsync(int count = 10)
        {
            return await _context.JobApplications
                .AsNoTracking() // Read-only query optimization
                .Include(j => j.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate name mapping
                .Include(j => j.JobPosition)
                .Include(j => j.AssignedRecruiter) // Include for recruiter name mapping
                .OrderByDescending(j => j.AppliedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Dictionary<ApplicationStatus, int>> GetApplicationStatusDistributionAsync(Guid? jobPositionId = null)
        {
            IQueryable<JobApplication> query = _context.JobApplications;

            if (jobPositionId.HasValue)
            {
                query = query.Where(a => a.JobPositionId == jobPositionId.Value);
            }

            return await query
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);
        }

        public async Task<IEnumerable<JobApplication>> GetApplicationsRequiringActionAsync(Guid? recruiterId = null)
        {
            var statusesRequiringAction = new[] { ApplicationStatus.Applied, ApplicationStatus.TestCompleted, ApplicationStatus.UnderReview };

            var query = _context.JobApplications
                .AsNoTracking() // Read-only query optimization
                .Include(a => a.CandidateProfile)
                    .ThenInclude(cp => cp.User) // Include User for candidate name mapping
                .Include(a => a.JobPosition)
                .Include(a => a.AssignedRecruiter)
                .Where(a => statusesRequiringAction.Contains(a.Status));

            if (recruiterId.HasValue)
            {
                query = query.Where(a => a.AssignedRecruiterId == recruiterId.Value);
            }

            return await query
                .OrderBy(a => a.AppliedDate) // Oldest first - needs attention
                .ToListAsync();
        }

        public async Task<bool> HasCandidateAppliedAsync(Guid jobPositionId, Guid candidateProfileId)
        {
            return await _context.JobApplications
                .AnyAsync(j => j.JobPositionId == jobPositionId && j.CandidateProfileId == candidateProfileId);
        }

        public async Task<int> GetActiveApplicationCountAsync(Guid candidateProfileId, IEnumerable<ApplicationStatus> activeStatuses)
        {
            var statusList = activeStatuses?.ToList() ?? new List<ApplicationStatus>();
            if (!statusList.Any())
            {
                return 0;
            }

            return await _context.JobApplications
                .Where(j => j.CandidateProfileId == candidateProfileId && statusList.Contains(j.Status))
                .CountAsync();
        }

        public async Task<JobApplication> UpdateAsync(JobApplication application)
        {
            application.UpdatedAt = DateTime.UtcNow;

            _context.JobApplications.Update(application);
            await _context.SaveChangesAsync();
            return application;
        }

        public async Task<JobApplication> UpdateStatusAsync(Guid applicationId, ApplicationStatus newStatus, Guid changedByUserId, string? comments = null)
        {
            var application = await _context.JobApplications
                .SingleAsync(a => a.Id == applicationId);

            var changedByUser = await _context.Users.FindAsync(changedByUserId);
            if (changedByUser == null)
                throw new ArgumentException($"User with ID {changedByUserId} not found", nameof(changedByUserId));

            var oldStatus = application.Status;
            application.Status = newStatus;
            application.UpdatedAt = DateTime.UtcNow;

            var statusHistory = new ApplicationStatusHistory
            {
                JobApplicationId = applicationId,
                JobApplication = application,
                FromStatus = oldStatus,
                ToStatus = newStatus,
                ChangedByUserId = changedByUserId,
                ChangedByUser = changedByUser,
                ChangedAt = DateTime.UtcNow,
                Comments = comments,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ApplicationStatusHistories.Add(statusHistory); // Add directly to context
            await _context.SaveChangesAsync();
            return application;
        }
    }
}
