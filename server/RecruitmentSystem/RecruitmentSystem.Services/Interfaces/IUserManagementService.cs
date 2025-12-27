using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IUserManagementService
    {
        // Search and view
        Task<PagedResult<UserSummaryDto>> SearchUsersAsync(UserSearchFilters filters);
        Task<UserDetailsDto?> GetUserDetailsAsync(Guid userId);

        // Bulk operations
        Task<UpdateUserStatusResult> UpdateUserStatusAsync(UpdateUserStatusRequest request, Guid adminUserId);
        Task<EndUserLockoutResult> EndUserLockoutAsync(EndUserLockoutRequest request, Guid adminUserId);
        Task<AdminResetPasswordResult> AdminResetPasswordAsync(AdminResetPasswordRequest request, Guid adminUserId);
        Task<ManageUserRolesResult> ManageUserRolesAsync(ManageUserRolesRequest request, Guid adminUserId);

        // Single operations
        Task<UserDetailsDto?> UpdateUserInfoAsync(Guid userId, UpdateUserInfoRequest request, Guid adminUserId);
    }
}
