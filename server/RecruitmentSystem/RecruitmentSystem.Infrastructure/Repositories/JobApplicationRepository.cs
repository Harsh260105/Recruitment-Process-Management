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
            application.AppliedDate = DateTime.UtcNow;
            application.CreatedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            _context.JobApplications.Add(application);
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

        public async Task<IEnumerable<JobApplication>> GetApplicationsWithFiltersAsync(ApplicationStatus? status = null, Guid? jobPositionId = null, Guid? candidateProfileId = null, Guid? assignedRecruiterId = null, DateTime? appliedFromDate = null, DateTime? appliedToDate = null)
        {
            var query = _context.JobApplications
                .Include(a => a.CandidateProfile)
                .Include(a => a.JobPosition)
                .Include(a => a.AssignedRecruiter)
                .AsQueryable();

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

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<JobApplication>> GetByCandidateIdAsync(Guid candidateProfileId)
        {
            return await _context.JobApplications
                .Include(j => j.CandidateProfile)
                .Include(j => j.JobPosition)
                .Where(j => j.CandidateProfileId == candidateProfileId)
                .ToListAsync();
        }

        public async Task<JobApplication?> GetByIdAsync(Guid id)
        {
            return await _context.JobApplications
                .Include(j => j.CandidateProfile)
                .Include(j => j.JobPosition)
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<JobApplication?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.JobApplications
                .Include(j => j.CandidateProfile)
                .Include(j => j.JobPosition)
                .Include(j => j.AssignedRecruiter)
                .Include(j => j.Interviews)
                    .ThenInclude(i => i.Participants)
                        .ThenInclude(iv => iv.ParticipantUser)
                .Include(j => j.StatusHistory)
                    .ThenInclude(sh => sh.ChangedByUser)
                .Include(j => j.JobOffer)
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<JobApplication?> GetByJobAndCandidateAsync(Guid jobPositionId, Guid candidateProfileId)
        {
            return await _context.JobApplications
                .Include(j => j.CandidateProfile)
                .Include(j => j.JobPosition)
                .FirstOrDefaultAsync(j => j.JobPositionId == jobPositionId && j.CandidateProfileId == candidateProfileId);
        }

        public async Task<IEnumerable<JobApplication>> GetByJobPositionIdAsync(Guid jobPositionId)
        {
            return await _context.JobApplications
                .Include(j => j.CandidateProfile)
                .Include(j => j.JobPosition)
                .Where(j => j.JobPositionId == jobPositionId)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobApplication>> GetByRecruiterAsync(Guid recruiterId)
        {
            return await _context.JobApplications
                .Include(j => j.CandidateProfile)
                .Include(j => j.JobPosition)
                .Where(j => j.AssignedRecruiterId == recruiterId)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobApplication>> GetByStatusAsync(ApplicationStatus status)
        {
            return await _context.JobApplications
                .Include(j => j.CandidateProfile)
                .Include(j => j.JobPosition)
                .Where(j => j.Status == status)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobApplication>> GetRecentApplicationsAsync(int count = 10)
        {
            return await _context.JobApplications
                .Include(j => j.CandidateProfile)
                .Include(j => j.JobPosition)
                .OrderByDescending(j => j.AppliedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<bool> HasCandidateAppliedAsync(Guid jobPositionId, Guid candidateProfileId)
        {
            return await _context.JobApplications
                .AnyAsync(j => j.JobPositionId == jobPositionId && j.CandidateProfileId == candidateProfileId);
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
                .Include(a => a.StatusHistory)
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
                Comments = comments
            };
            
            application.StatusHistory.Add(statusHistory);
            await _context.SaveChangesAsync();
            return application;
        }
    }
}
