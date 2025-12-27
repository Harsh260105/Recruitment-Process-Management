using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IUserManagementRepository
    {
        // User queries
        Task<(List<User> Items, int TotalCount)> SearchUsersAsync(
            string? searchTerm,
            List<string>? roles,
            bool? isActive,
            bool? hasProfile,
            int pageNumber,
            int pageSize);

        Task<User?> GetUserWithDetailsAsync(Guid userId);

        // Role validation
        Task<List<string>> GetExistingRolesAsync(List<string> roleNames);
    }
}
