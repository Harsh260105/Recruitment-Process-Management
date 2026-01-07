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

        public async Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> GetSummariesAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
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
            var query = _context.JobPositions
                .AsNoTracking();

            query = ApplyFilters(
                    query,
                    searchTerm,
                    status,
                    department,
                    location,
                    experienceLevel,
                    skillIds,
                    createdFromDate,
                    createdToDate,
                    deadlineFromDate,
                    deadlineToDate)
                .OrderByDescending(j => j.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        #endregion

        #region Helpers

        private static IQueryable<JobPosition> ApplyFilters(
            IQueryable<JobPosition> query,
            string? searchTerm,
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
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchPattern = BuildContainsPattern(searchTerm);
                query = query.Where(j =>
                    (j.Title != null && EF.Functions.Like(j.Title, searchPattern)) ||
                    (j.Description != null && EF.Functions.Like(j.Description, searchPattern)) ||
                    (j.RequiredQualifications != null && EF.Functions.Like(j.RequiredQualifications, searchPattern)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active")
                {
                    query = query.Where(j => j.Status == "Active" && (j.ApplicationDeadline == null || j.ApplicationDeadline > DateTime.UtcNow));
                }
                else
                {
                    query = query.Where(j => j.Status == status);
                }
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                var departmentPattern = BuildContainsPattern(department);
                query = query.Where(j => j.Department != null && EF.Functions.Like(j.Department, departmentPattern));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var locationPattern = BuildContainsPattern(location);
                query = query.Where(j => j.Location != null && EF.Functions.Like(j.Location, locationPattern));
            }

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

        private static string BuildContainsPattern(string value)
        {
            var trimmed = value.Trim();
            return string.IsNullOrEmpty(trimmed) ? "%" : $"%{trimmed}%";
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