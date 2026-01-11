using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class UserManagementRepository : IUserManagementRepository
    {
        private readonly ApplicationDbContext _context;

        public UserManagementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<User> Items, int TotalCount)> SearchUsersAsync(
            string? searchTerm,
            List<string>? roles,
            bool? isActive,
            bool? hasProfile,
            int pageNumber,
            int pageSize)
        {
            var queryable = _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.CandidateProfile)
                .Include(u => u.StaffProfile)
                .AsQueryable();

            // Filter by search term (name or email)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                queryable = queryable.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchLower)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchLower))
                );
            }

            // Filter by roles
            if (roles?.Any() == true)
            {
                var rolesToFilter = roles.Select(r => r.ToLower()).ToList();
                queryable = queryable.Where(u =>
                    u.UserRoles.Any(ur => ur.Role != null && rolesToFilter.Contains(ur.Role.Name!.ToLower()))
                );
            }

            // Filter by active status
            if (isActive.HasValue)
            {
                queryable = queryable.Where(u => u.IsActive == isActive.Value);
            }

            // Filter by profile completion (only for candidates)
            if (hasProfile.HasValue)
            {
                if (hasProfile.Value)
                {
                    queryable = queryable.Where(u => u.CandidateProfile != null || u.StaffProfile != null);
                }
                else
                {
                    queryable = queryable.Where(u => u.CandidateProfile == null && u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Candidate"));
                }
            }

            // Get total count
            var totalCount = await queryable.CountAsync();

            // Apply pagination and ordering
            var users = await queryable
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<User?> GetUserWithDetailsAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return null;

            return await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.CandidateProfile)
                .Include(u => u.StaffProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<List<string>> GetExistingRolesAsync(List<string> roleNames)
        {
            return await _context.Roles
                .Where(r => roleNames.Contains(r.Name!))
                .Select(r => r.Name!)
                .ToListAsync();
        }
    }
}
