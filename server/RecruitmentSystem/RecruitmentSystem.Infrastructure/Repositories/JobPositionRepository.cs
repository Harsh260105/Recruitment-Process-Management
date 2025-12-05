using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Entities.Projections;
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

        public async Task<bool> IsJobPositionAvailableForApplicationAsync(Guid jobPositionId)
        {
            var jobPosition = await _context.JobPositions.FindAsync(jobPositionId);
            if (jobPosition == null)
                return false;

            if (jobPosition.Status != "Active")
                return false;

            if (jobPosition.ApplicationDeadline.HasValue && jobPosition.ApplicationDeadline.Value < DateTime.UtcNow)
                return false;

            if (jobPosition.ClosedDate.HasValue)
                return false;

            return true;
        }

        public async Task<JobPosition?> GetByIdAsync(Guid id)
        {
            return await _context.JobPositions
                .AsNoTracking()
                .Include(j => j.CreatedByUser)
                .Include(j => j.JobPositionSkills)
                    .ThenInclude(jps => jps.Skill)
                .FirstOrDefaultAsync(j => j.Id == id);
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

        public async Task IncrementTotalApplicantsAsync(Guid jobPositionId)
        {
            await _context.JobPositions
                .Where(j => j.Id == jobPositionId)
                .ExecuteUpdateAsync(j => j.SetProperty(p => p.TotalApplicants, p => p.TotalApplicants + 1));
        }

        #region Pagination Methods

        public async Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> GetPositionSummariesWithFiltersAsync(
            int pageNumber, int pageSize,
            string? status = null,
            string? department = null,
            string? location = null,
            string? experienceLevel = null,
            List<int>? skillIds = null,
            DateTime? createdFromDate = null,
            DateTime? createdToDate = null,
            DateTime? deadlineFromDate = null,
            DateTime? deadlineToDate = null)
        {
            var query = ApplyCommonFilters(_context.JobPositions.AsNoTracking(), status, department, location,
                experienceLevel, skillIds, createdFromDate, createdToDate, deadlineFromDate, deadlineToDate)
                .OrderByDescending(j => j.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> GetActiveSummariesAsync(int pageNumber, int pageSize)
        {
            var query = _context.JobPositions
                .Where(j => j.Status == "Active" && j.ApplicationDeadline > DateTime.UtcNow)
                .OrderByDescending(j => j.CreatedAt)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> SearchPositionSummariesAsync(
            string searchTerm, int pageNumber, int pageSize, string? department = null, string? status = null)
        {
            var query = ApplySearchFilters(BuildSearchQuery(searchTerm).AsNoTracking(), department, status)
                .OrderByDescending(j => j.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // public async Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> GetSummaryByDepartmentAsync(string department, int pageNumber, int pageSize)
        // {
        //     var query = _context.JobPositions
        //         .Where(j => j.Department == department)
        //         .OrderByDescending(j => j.CreatedAt)
        //         .AsNoTracking();

        //     var totalCount = await query.CountAsync();
        //     var items = await ProjectToSummary(query)
        //         .Skip((pageNumber - 1) * pageSize)
        //         .Take(pageSize)
        //         .ToListAsync();

        //     return (items, totalCount);
        // }

        // public async Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> GetSummaryByStatusAsync(string status, int pageNumber, int pageSize)
        // {
        //     var query = _context.JobPositions
        //         .Where(j => j.Status == status)
        //         .OrderByDescending(j => j.CreatedAt)
        //         .AsNoTracking();

        //     var totalCount = await query.CountAsync();
        //     var items = await ProjectToSummary(query)
        //         .Skip((pageNumber - 1) * pageSize)
        //         .Take(pageSize)
        //         .ToListAsync();

        //     return (items, totalCount);
        // }

        #endregion

        #region Helpers

        private static IQueryable<JobPosition> ApplyCommonFilters(
            IQueryable<JobPosition> query,
            string? status,
            string? department,
            string? location,
            string? experienceLevel,
            List<int>? skillIds,
            DateTime? createdFromDate,
            DateTime? createdToDate,
            DateTime? deadlineFromDate,
            DateTime? deadlineToDate)
        {
            if (!string.IsNullOrEmpty(status))
                query = query.Where(j => j.Status == status);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(j => j.Department == department);

            if (!string.IsNullOrEmpty(location))
                query = query.Where(j => j.Location == location);

            if (!string.IsNullOrEmpty(experienceLevel))
                query = query.Where(j => j.ExperienceLevel == experienceLevel);

            if (skillIds != null && skillIds.Any())
                query = query.Where(j => j.JobPositionSkills.Any(js => skillIds.Contains(js.SkillId)));

            if (createdFromDate.HasValue)
                query = query.Where(j => j.CreatedAt >= createdFromDate.Value);

            if (createdToDate.HasValue)
                query = query.Where(j => j.CreatedAt <= createdToDate.Value);

            if (deadlineFromDate.HasValue)
                query = query.Where(j => j.ApplicationDeadline >= deadlineFromDate.Value);

            if (deadlineToDate.HasValue)
                query = query.Where(j => j.ApplicationDeadline <= deadlineToDate.Value);

            return query;
        }

        private IQueryable<JobPosition> BuildSearchQuery(string searchTerm)
        {
            return _context.JobPositions
                .Where(j => (j.Title != null && j.Title.Contains(searchTerm)) ||
                            (j.Description != null && j.Description.Contains(searchTerm)) ||
                            (j.RequiredQualifications != null && j.RequiredQualifications.Contains(searchTerm)));
        }

        private static IQueryable<JobPosition> ApplySearchFilters(
            IQueryable<JobPosition> query,
            string? department,
            string? status)
        {
            if (!string.IsNullOrEmpty(department))
                query = query.Where(j => j.Department == department);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(j => j.Status == status);

            return query;
        }

        private static IQueryable<JobPositionSummaryProjection> ProjectToSummary(IQueryable<JobPosition> query)
        {
            return query.Select(j => new JobPositionSummaryProjection
            {
                Id = j.Id,
                Title = j.Title,
                Department = j.Department,
                Location = j.Location,
                EmploymentType = j.EmploymentType,
                ExperienceLevel = j.ExperienceLevel,
                SalaryRange = j.SalaryRange,
                Status = j.Status,
                ApplicationDeadline = j.ApplicationDeadline,
                MinExperience = j.MinExperience,
                TotalApplicants = j.TotalApplicants,
                CreatedAt = j.CreatedAt,
                UpdatedAt = j.UpdatedAt,
                CreatedByUserId = j.CreatedByUserId,
                CreatorFirstName = j.CreatedByUser != null ? j.CreatedByUser.FirstName : null,
                CreatorLastName = j.CreatedByUser != null ? j.CreatedByUser.LastName : null,
                CreatorEmail = j.CreatedByUser != null ? j.CreatedByUser.Email : null,
                Skills = j.JobPositionSkills
                    .Select(js => new JobPositionSummarySkillProjection
                    {
                        SkillId = js.SkillId,
                        SkillName = js.Skill != null ? js.Skill.Name : null,
                        IsRequired = js.IsRequired
                    })
                    .ToList()
            });
        }

        #endregion
    }
}