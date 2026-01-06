using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class StaffProfileRepository : IStaffProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public StaffProfileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StaffProfile> CreateAsync(StaffProfile profile)
        {
            profile.CreatedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            _context.StaffProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var profile = await _context.StaffProfiles.FindAsync(id);

            _context.StaffProfiles.Remove(profile!);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.StaffProfiles.AnyAsync(sp => sp.Id == id);
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId)
        {
            return await _context.StaffProfiles.AnyAsync(sp => sp.UserId == userId);
        }

        public async Task<IEnumerable<StaffProfile>> GetActiveStaffAsync()
        {
            return await _context.StaffProfiles
                .AsNoTracking() // Read-only query optimization
                .Where(sp => sp.Status == "Active")
                .ToListAsync();
        }

        public async Task<IEnumerable<StaffProfile>> GetByDepartmentAsync(string department)
        {
            return await _context.StaffProfiles
                .AsNoTracking() // Read-only query optimization
                .Where(sp => sp.Department == department)
                .ToListAsync();
        }

        public async Task<StaffProfile?> GetByEmployeeCodeAsync(string employeeCode)
        {
            return await _context.StaffProfiles
                .AsNoTracking() // Read-only query optimization
                .FirstOrDefaultAsync(sp => sp.EmployeeCode == employeeCode);
        }

        public async Task<StaffProfile?> GetByIdAsync(Guid id)
        {
            return await _context.StaffProfiles
                .AsNoTracking() // Read-only query optimization
                .Include(sp => sp.User)
                .FirstOrDefaultAsync(sp => sp.Id == id);
        }

        public async Task<StaffProfile?> GetByUserIdAsync(Guid userId)
        {
            return await _context.StaffProfiles
                .AsNoTracking() // Read-only query optimization
                .Include(sp => sp.User)
                .FirstOrDefaultAsync(sp => sp.UserId == userId);
        }

        public async Task<(List<StaffProfile> Items, int TotalCount)> SearchStaffAsync(
            string? query,
            string? department,
            string? location,
            IEnumerable<string>? roles,
            string? status,
            int pageNumber,
            int pageSize)
        {
            var normalizedRoles = roles?
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .ToList() ?? new List<string> { "Recruiter", "HR" };

            var staffQuery = _context.StaffProfiles
                .AsNoTracking()
                .Include(sp => sp.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                staffQuery = staffQuery.Where(sp => sp.Status == status);
            }
            else
            {
                staffQuery = staffQuery.Where(sp => sp.Status == "Active");
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                var deptTerm = department.Trim();
                staffQuery = staffQuery.Where(sp => sp.Department != null && sp.Department.Contains(deptTerm));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var locTerm = location.Trim();
                staffQuery = staffQuery.Where(sp => sp.Location != null && sp.Location.Contains(locTerm));
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var searchTerm = query.Trim();
                staffQuery = staffQuery.Where(sp =>
                    (sp.User.FirstName != null && sp.User.FirstName.Contains(searchTerm)) ||
                    (sp.User.LastName != null && sp.User.LastName.Contains(searchTerm)) ||
                    (sp.User.Email != null && sp.User.Email.Contains(searchTerm)) ||
                    (sp.EmployeeCode != null && sp.EmployeeCode.Contains(searchTerm)));
            }

            if (normalizedRoles != null && normalizedRoles.Count > 0)
            {
                // Rely on database collation for case-insensitive comparison instead of calling ToUpperInvariant() in the expression
                staffQuery = staffQuery.Where(sp =>
                    sp.User.UserRoles.Any(ur => ur.Role.Name != null && normalizedRoles.Contains(ur.Role.Name)));
            }

            // Get total count before pagination
            var totalCount = await staffQuery.CountAsync();

            // Apply pagination
            var items = await staffQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<StaffProfile> UpdateAsync(StaffProfile profile)
        {
            profile.UpdatedAt = DateTime.UtcNow;

            _context.StaffProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return profile;
        }
    }
}