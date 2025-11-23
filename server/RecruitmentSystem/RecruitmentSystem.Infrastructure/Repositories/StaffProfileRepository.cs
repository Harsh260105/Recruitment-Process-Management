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

        public async Task<StaffProfile> UpdateAsync(StaffProfile profile)
        {
            profile.UpdatedAt = DateTime.UtcNow;

            _context.StaffProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return profile;
        }
    }
}