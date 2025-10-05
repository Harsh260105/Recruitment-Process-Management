using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Infrastructure.Data;
using RecruitmentSystem.Core.Interfaces;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class JobPositionRepository : IJobPositionRepository
    {

        private readonly ApplicationDbContext _context;

        public JobPositionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<JobPosition> CreateAsync(JobPosition job)
        {
            job.CreatedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            _context.JobPositions.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var job = await _context.JobPositions.FindAsync(id);
            if (job == null) return false;

            _context.JobPositions.Remove(job);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.JobPositions.AnyAsync(j => j.Id == id);
        }

        public async Task<IEnumerable<JobPosition>> GetActiveAsync()
        {
            return await _context.JobPositions
                .Include(j => j.CreatedByUser)
                .Include(j => j.JobPositionSkills)
                    .ThenInclude(jps => jps.Skill)
                .Where(j => j.Status == "Active")
                .ToListAsync();
        }

        public async Task<IEnumerable<JobPosition>> GetByDepartmentAsync(string department)
        {
            return await _context.JobPositions
                .Include(j => j.CreatedByUser)
                .Include(j => j.JobPositionSkills)
                    .ThenInclude(jps => jps.Skill)
                .Where(j => j.Department == department)
                .ToListAsync();
        }

        public async Task<JobPosition?> GetByIdAsync(Guid id)
        {
            return await _context.JobPositions
                .Include(j => j.CreatedByUser)
                .Include(j => j.JobPositionSkills)
                    .ThenInclude(jps => jps.Skill)
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<IEnumerable<JobPosition>> GetByStatusAsync(string status)
        {
            return await _context.JobPositions
                .Include(j => j.CreatedByUser)
                .Include(j => j.JobPositionSkills)
                    .ThenInclude(jps => jps.Skill)
                .Where(j => j.Status == status)
                .ToListAsync();
        }

        public async Task<List<JobPosition>> GetPositionsWithFiltersAsync(string? status = null, string? department = null, string? location = null, string? experienceLevel = null, List<int>? skillIds = null, DateTime? createdFromDate = null, DateTime? createdToDate = null, DateTime? deadlineFromDate = null, DateTime? deadlineToDate = null)
        {
            var query = _context.JobPositions
                .Include(j => j.CreatedByUser)
                .Include(j => j.JobPositionSkills)
                    .ThenInclude(jps => jps.Skill)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(j => j.Status == status);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(j => j.Department == department);

            if (!string.IsNullOrEmpty(location))
                query = query.Where(j => j.Location == location);

            if (!string.IsNullOrEmpty(experienceLevel))
                query = query.Where(j => j.ExperienceLevel == experienceLevel);

            if (skillIds != null && skillIds.Any())
                query = query.Where(j => j.JobPositionSkills.Any(jps => skillIds.Contains(jps.SkillId)));

            if (createdFromDate.HasValue)
                query = query.Where(j => j.CreatedAt >= createdFromDate.Value);

            if (createdToDate.HasValue)
                query = query.Where(j => j.CreatedAt <= createdToDate.Value);

            if (deadlineFromDate.HasValue)
                query = query.Where(j => j.ApplicationDeadline >= deadlineFromDate.Value);

            if (deadlineToDate.HasValue)
                query = query.Where(j => j.ApplicationDeadline <= deadlineToDate.Value);

            return await query.ToListAsync();
        }

        public async Task<List<JobPosition>> SearchPositionsAsync(string searchTerm, string? department = null, string? status = null)
        {
            var query = _context.JobPositions
                .Include(j => j.CreatedByUser)
                .Include(j => j.JobPositionSkills)
                    .ThenInclude(jps => jps.Skill)
                .AsQueryable();

            // Search in title and description
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(j => j.Title.Contains(searchTerm) || j.Description.Contains(searchTerm));

            if (!string.IsNullOrEmpty(department))
                query = query.Where(j => j.Department == department);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(j => j.Status == status);

            return await query.ToListAsync();
        }

        public async Task<JobPosition> UpdateAsync(JobPosition job)
        {
            job.UpdatedAt = DateTime.UtcNow;

            _context.JobPositions.Update(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task AddSkillsAsync(IEnumerable<JobPositionSkill> skills)
        {
            await _context.JobPositionSkills.AddRangeAsync(skills);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveSkillsAsync(Guid jobPositionId)
        {
            var skills = _context.JobPositionSkills.Where(s => s.JobPositionId == jobPositionId);
            _context.JobPositionSkills.RemoveRange(skills);
            await _context.SaveChangesAsync();
        }
    }
}