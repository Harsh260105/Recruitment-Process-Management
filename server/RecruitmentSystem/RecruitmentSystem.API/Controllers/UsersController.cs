using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public class UsersController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;

        public UsersController(
            IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim!);
        }

        /// <summary>
        /// Search and filter all users across roles (Admin only)
        /// </summary>
        /// <param name="filters">Search and filter parameters</param>
        /// <returns>Paginated list of users</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<UserSummaryDto>>>> SearchUsers(
            [FromQuery] UserSearchFilters filters)
        {
            try
            {
                // Validate pagination parameters
                if (filters.PageNumber < 1) filters.PageNumber = 1;
                if (filters.PageSize < 1) filters.PageSize = 25;
                if (filters.PageSize > 100) filters.PageSize = 100;

                List<string> allowedRoles;
                if (User.IsInRole("SuperAdmin"))
                {
                    allowedRoles = new List<string> { "SuperAdmin", "Admin", "HR", "Recruiter", "Candidate" };
                }
                else if (User.IsInRole("Admin"))
                {
                    allowedRoles = new List<string> { "Admin", "HR", "Recruiter", "Candidate" };
                }
                else if (User.IsInRole("HR"))
                {
                    allowedRoles = new List<string> { "HR", "Recruiter", "Candidate" };
                }
                else
                {
                    allowedRoles = new List<string>();
                }

                // Filter
                if (filters.Roles == null || !filters.Roles.Any())
                {
                    filters.Roles = allowedRoles;
                }
                else
                {
                    filters.Roles = filters.Roles.Intersect(allowedRoles).ToList();
                }

                var result = await _userManagementService.SearchUsersAsync(filters);

                return Ok(ApiResponse<PagedResult<UserSummaryDto>>.SuccessResponse(
                    result,
                    $"Found {result.TotalCount} users"
                ));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<PagedResult<UserSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while searching users" },
                    "Search Failed"
                ));
            }
        }

        /// <summary>
        /// Get detailed information about a specific user (Admin only)
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>User details</returns>
        [HttpGet("{userId}")]
        public async Task<ActionResult<ApiResponse<UserDetailsDto>>> GetUserDetails(Guid userId)
        {
            try
            {
                var user = await _userManagementService.GetUserDetailsAsync(userId);

                if (user == null)
                {
                    return NotFound(ApiResponse<UserDetailsDto>.FailureResponse(
                        new List<string> { "User not found" },
                        "Not Found"
                    ));
                }

                return Ok(ApiResponse<UserDetailsDto>.SuccessResponse(
                    user,
                    "User details retrieved successfully"
                ));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<UserDetailsDto>.FailureResponse(
                    new List<string> { "An error occurred while retrieving user details" },
                    "Retrieval Failed"
                ));
            }
        }

        /// <summary>
        /// Bulk activate or deactivate user accounts (Admin only)
        /// </summary>
        /// <param name="request">List of user IDs and desired status</param>
        /// <returns>Result of bulk operation</returns>
        [HttpPatch("status")]
        public async Task<ActionResult<ApiResponse<UpdateUserStatusResult>>> UpdateUserStatus(
            [FromBody] UpdateUserStatusRequest request)
        {
            try
            {
                if (request.UserIds == null || !request.UserIds.Any())
                {
                    return BadRequest(ApiResponse<UpdateUserStatusResult>.FailureResponse(
                        new List<string> { "At least one user ID is required" },
                        "Invalid Request"
                    ));
                }

                var adminUserId = GetCurrentUserId();
                var result = await _userManagementService.UpdateUserStatusAsync(request, adminUserId);

                var message = $"Status update completed. Success: {result.SuccessCount}, Failed: {result.FailureCount}";

                return Ok(ApiResponse<UpdateUserStatusResult>.SuccessResponse(
                    result,
                    message
                ));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<UpdateUserStatusResult>.FailureResponse(
                    new List<string> { "An error occurred while updating user status" },
                    "Update Failed"
                ));
            }
        }

        /// <summary>
        /// End lockout for locked out user accounts (Admin only)
        /// </summary>
        /// <param name="request">List of user IDs to end lockout for</param>
        /// <returns>Result of bulk operation</returns>
        [HttpPost("end-lockout")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse<EndUserLockoutResult>>> EndLockout(
            [FromBody] EndUserLockoutRequest request)
        {
            try
            {
                if (request.UserIds == null || !request.UserIds.Any())
                {
                    return BadRequest(ApiResponse<EndUserLockoutResult>.FailureResponse(
                        new List<string> { "At least one user ID is required" },
                        "Invalid Request"
                    ));
                }

                var adminUserId = GetCurrentUserId();
                var result = await _userManagementService.EndUserLockoutAsync(request, adminUserId);

                return Ok(ApiResponse<EndUserLockoutResult>.SuccessResponse(result, "Lockout ended successfully"));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<EndUserLockoutResult>.FailureResponse(
                    new List<string> { "An error occurred while ending user lockout" },
                    "End Lockout Failed"
                ));
            }
        }

        /// <summary>
        /// Admin force password reset for users (bulk operation)
        /// </summary>
        /// <param name="request">List of user IDs to reset passwords for</param>
        /// <returns>Result with temporary passwords</returns>
        [HttpPost("reset-password")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse<AdminResetPasswordResult>>> ResetPassword(
            [FromBody] AdminResetPasswordRequest request)
        {
            try
            {
                if (request.UserIds == null || !request.UserIds.Any())
                {
                    return BadRequest(ApiResponse<AdminResetPasswordResult>.FailureResponse(
                        new List<string> { "At least one user ID is required" },
                        "Invalid Request"
                    ));
                }

                var adminUserId = GetCurrentUserId();
                var result = await _userManagementService.AdminResetPasswordAsync(request, adminUserId);

                var message = $"Password reset completed. Success: {result.SuccessCount}, Failed: {result.FailureCount}";

                return Ok(ApiResponse<AdminResetPasswordResult>.SuccessResponse(
                    result,
                    message
                ));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<AdminResetPasswordResult>.FailureResponse(
                    new List<string> { "An error occurred while resetting passwords" },
                    "Reset Failed"
                ));
            }
        }

        /// <summary>
        /// Update user basic information (Admin only)
        /// </summary>
        /// <param name="userId">The user ID to update</param>
        /// <param name="request">Updated user information</param>
        /// <returns>Updated user details</returns>
        [HttpPatch("{userId}")]
        public async Task<ActionResult<ApiResponse<UserDetailsDto>>> UpdateUserInfo(
            Guid userId,
            [FromBody] UpdateUserInfoRequest request)
        {
            try
            {
                if (request == null || (string.IsNullOrWhiteSpace(request.FirstName) &&
                    string.IsNullOrWhiteSpace(request.LastName) &&
                    request.PhoneNumber == null))
                {
                    return BadRequest(ApiResponse<UserDetailsDto>.FailureResponse(
                        new List<string> { "At least one field must be provided to update" },
                        "Invalid Request"
                    ));
                }

                var adminUserId = GetCurrentUserId();
                var updatedUser = await _userManagementService.UpdateUserInfoAsync(userId, request, adminUserId);

                if (updatedUser == null)
                {
                    return NotFound(ApiResponse<UserDetailsDto>.FailureResponse(
                        new List<string> { "User not found or update failed" },
                        "Update Failed"
                    ));
                }

                return Ok(ApiResponse<UserDetailsDto>.SuccessResponse(
                    updatedUser,
                    "User information updated successfully"
                ));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<UserDetailsDto>.FailureResponse(
                    new List<string> { "An error occurred while updating user information" },
                    "Update Failed"
                ));
            }
        }

        /// <summary>
        /// Bulk add or remove roles for users (Admin only)
        /// </summary>
        /// <param name="request">User IDs and roles to add/remove</param>
        /// <returns>Result of bulk role management</returns>
        [HttpPost("roles")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse<ManageUserRolesResult>>> ManageUserRoles(
            [FromBody] ManageUserRolesRequest request)
        {
            try
            {
                if (request.UserIds == null || !request.UserIds.Any())
                {
                    return BadRequest(ApiResponse<ManageUserRolesResult>.FailureResponse(
                        new List<string> { "At least one user ID is required" },
                        "Invalid Request"
                    ));
                }

                if ((request.RolesToAdd == null || !request.RolesToAdd.Any()) &&
                    (request.RolesToRemove == null || !request.RolesToRemove.Any()))
                {
                    return BadRequest(ApiResponse<ManageUserRolesResult>.FailureResponse(
                        new List<string> { "At least one role to add or remove is required" },
                        "Invalid Request"
                    ));
                }

                var adminUserId = GetCurrentUserId();
                var result = await _userManagementService.ManageUserRolesAsync(request, adminUserId);

                var message = $"Role management completed. Success: {result.SuccessCount}, Failed: {result.FailureCount}";

                return Ok(ApiResponse<ManageUserRolesResult>.SuccessResponse(
                    result,
                    message
                ));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<ManageUserRolesResult>.FailureResponse(
                    new List<string> { "An error occurred while managing user roles" },
                    "Management Failed"
                ));
            }
        }
    }
}
