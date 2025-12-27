using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using AutoMapper;

namespace RecruitmentSystem.Services.Implementations
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserManagementRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            IUserManagementRepository repository,
            ApplicationDbContext context,
            UserManager<User> userManager,
            IEmailService emailService,
            IMapper mapper,
            ILogger<UserManagementService> logger)
        {
            _repository = repository;
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<UserSummaryDto>> SearchUsersAsync(UserSearchFilters filters)
        {
            try
            {
                var (users, totalCount) = await _repository.SearchUsersAsync(
                    filters.Search,
                    filters.Roles,
                    filters.IsActive,
                    filters.HasProfile,
                    filters.PageNumber,
                    filters.PageSize);

                var userDtos = _mapper.Map<List<UserSummaryDto>>(users);

                return PagedResult<UserSummaryDto>.Create(
                    userDtos,
                    totalCount,
                    filters.PageNumber,
                    filters.PageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with filters: {@Filters}", filters);
                throw;
            }
        }

        public async Task<UserDetailsDto?> GetUserDetailsAsync(Guid userId)
        {
            try
            {
                var user = await _repository.GetUserWithDetailsAsync(userId);

                if (user == null)
                    return null;

                return _mapper.Map<UserDetailsDto>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user details for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<UpdateUserStatusResult> UpdateUserStatusAsync(UpdateUserStatusRequest request, Guid adminUserId)
        {
            var result = new UpdateUserStatusResult();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var userId in request.UserIds)
                {
                    try
                    {
                        var user = await _userManager.FindByIdAsync(userId.ToString());
                        if (user == null)
                        {
                            result.FailureCount++;
                            result.Errors.Add($"User {userId} not found");
                            continue;
                        }

                        // Prevent deactivating self
                        if (userId == adminUserId && !request.IsActive)
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Cannot deactivate your own account");
                            continue;
                        }

                        // Prevent deactivating super admin unless you are super admin
                        var userRoles = await _userManager.GetRolesAsync(user);
                        if (userRoles.Contains("SuperAdmin") && !request.IsActive)
                        {
                            var adminUser = await _userManager.FindByIdAsync(adminUserId.ToString());
                            var adminRoles = await _userManager.GetRolesAsync(adminUser!);
                            if (!adminRoles.Contains("SuperAdmin"))
                            {
                                result.FailureCount++;
                                result.Errors.Add($"Only SuperAdmin can deactivate another SuperAdmin");
                                continue;
                            }
                        }

                        user.IsActive = request.IsActive;
                        var updateResult = await _userManager.UpdateAsync(user);

                        if (updateResult.Succeeded)
                        {
                            result.SuccessCount++;
                            _logger.LogInformation(
                                "Admin {AdminId} {Action} user {UserId}",
                                adminUserId,
                                request.IsActive ? "activated" : "deactivated",
                                userId
                            );
                        }
                        else
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Failed to update user {userId}: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Error updating user {userId}: {ex.Message}");
                        _logger.LogError(ex, "Error updating status for user {UserId}", userId);
                    }
                }

                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bulk user status update by admin {AdminId}", adminUserId);
                throw;
            }
        }

        public async Task<EndUserLockoutResult> EndUserLockoutAsync(EndUserLockoutRequest request, Guid adminUserId)
        {
            var result = new EndUserLockoutResult();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var userId in request.UserIds)
                {
                    try
                    {
                        var user = await _userManager.FindByIdAsync(userId.ToString());
                        if (user == null)
                        {
                            result.FailureCount++;
                            result.Errors.Add($"User {userId} not found");
                            continue;
                        }

                        // Check if user is currently locked out
                        if (await _userManager.IsLockedOutAsync(user))
                        {
                            // Reset access failed count and end lockout
                            var resetResult = await _userManager.ResetAccessFailedCountAsync(user);
                            var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);

                            if (resetResult.Succeeded && lockoutResult.Succeeded)
                            {
                                result.SuccessCount++;
                                _logger.LogInformation(
                                    "Admin {AdminId} ended lockout for user {UserId}",
                                    adminUserId,
                                    userId
                                );
                            }
                            else
                            {
                                result.FailureCount++;
                                var errors = resetResult.Errors.Concat(lockoutResult.Errors)
                                    .Select(e => e.Description);
                                result.Errors.Add($"Failed to end lockout for user {userId}: {string.Join(", ", errors)}");
                            }
                        }
                        else
                        {
                            // User is not locked out, consider this a success
                            result.SuccessCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Error ending lockout for user {userId}: {ex.Message}");
                        _logger.LogError(ex, "Error ending lockout for user {UserId}", userId);
                    }
                }

                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bulk end lockout by admin {AdminId}", adminUserId);
                throw;
            }
        }

        public async Task<AdminResetPasswordResult> AdminResetPasswordAsync(AdminResetPasswordRequest request, Guid adminUserId)
        {
            var result = new AdminResetPasswordResult();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var userId in request.UserIds)
                {
                    try
                    {
                        var user = await _userManager.FindByIdAsync(userId.ToString());
                        if (user == null)
                        {
                            result.FailureCount++;
                            result.Errors.Add($"User {userId} not found");
                            continue;
                        }

                        // Prevent resetting super admin password unless you are super admin
                        var userRoles = await _userManager.GetRolesAsync(user);
                        if (userRoles.Contains("SuperAdmin"))
                        {
                            var adminUser = await _userManager.FindByIdAsync(adminUserId.ToString());
                            var adminRoles = await _userManager.GetRolesAsync(adminUser!);
                            if (!adminRoles.Contains("SuperAdmin"))
                            {
                                result.FailureCount++;
                                result.Errors.Add($"Only SuperAdmin can reset another SuperAdmin's password");
                                continue;
                            }
                        }

                        // Generate temporary password
                        var tempPassword = GenerateTemporaryPassword(user.FirstName, user.LastName, user.Email);

                        // Remove existing password and set new one
                        var removeResult = await _userManager.RemovePasswordAsync(user);
                        if (!removeResult.Succeeded)
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Failed to reset password for user {userId}");
                            continue;
                        }

                        var addResult = await _userManager.AddPasswordAsync(user, tempPassword);
                        if (addResult.Succeeded)
                        {
                            result.SuccessCount++;
                            result.ResetInfo.Add(new PasswordResetInfo
                            {
                                UserId = user.Id,
                                Email = user.Email ?? string.Empty,
                                TemporaryPassword = null // Never expose temporary passwords in API responses for security
                            });

                            _logger.LogInformation(
                                "Admin {AdminId} reset password for user {UserId}",
                                adminUserId,
                                userId
                            );

                            // Send temporary password by email if requested
                            if (request.SendEmail)
                            {
                                try
                                {
                                    var subject = "Your temporary password";
                                    var htmlBody = $@"<p>Hello {System.Net.WebUtility.HtmlEncode(user.FirstName ?? string.Empty)},</p>
<p>An administrator has reset your account password. Your temporary password is:</p>
<p style='font-weight:bold;'>{System.Net.WebUtility.HtmlEncode(tempPassword)}</p>
<p>Please log in and change your password immediately.</p>";

                                    var sendResult = await _emailService.SendEmailAsync(user.Email ?? string.Empty, subject, htmlBody);
                                    if (!sendResult)
                                    {
                                        result.Errors.Add($"Failed to send password email to {user.Email}");
                                        _logger.LogWarning("Failed to send temporary password email to {Email} for user {UserId}", user.Email, user.Id);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    result.Errors.Add($"Error sending password email to {user.Email}: {ex.Message}");
                                    _logger.LogError(ex, "Error sending temporary password email for user {UserId}", user.Id);
                                }
                            }
                        }
                        else
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Failed to set new password for user {userId}: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Error resetting password for user {userId}: {ex.Message}");
                        _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
                    }
                }

                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bulk password reset by admin {AdminId}", adminUserId);
                throw;
            }
        }

        public async Task<ManageUserRolesResult> ManageUserRolesAsync(ManageUserRolesRequest request, Guid adminUserId)
        {
            var result = new ManageUserRolesResult();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate that all roles exist
                var allRoleNames = request.RolesToAdd.Concat(request.RolesToRemove).Distinct().ToList();
                var existingRoles = await _repository.GetExistingRolesAsync(allRoleNames);

                var invalidRoles = allRoleNames.Except(existingRoles).ToList();
                if (invalidRoles.Any())
                {
                    result.Errors.Add($"Invalid roles: {string.Join(", ", invalidRoles)}");
                    return result;
                }

                foreach (var userId in request.UserIds)
                {
                    try
                    {
                        var user = await _userManager.FindByIdAsync(userId.ToString());
                        if (user == null)
                        {
                            result.FailureCount++;
                            result.Errors.Add($"User {userId} not found");
                            continue;
                        }

                        // Get admin and user roles for permission checks
                        var adminUser = await _userManager.FindByIdAsync(adminUserId.ToString());
                        var adminRoles = await _userManager.GetRolesAsync(adminUser!);
                        var userRoles = await _userManager.GetRolesAsync(user);

                        // SuperAdmin role changes require SuperAdmin permission
                        if ((request.RolesToAdd.Contains("SuperAdmin") || request.RolesToRemove.Contains("SuperAdmin") || userRoles.Contains("SuperAdmin"))
                            && !adminRoles.Contains("SuperAdmin"))
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Only SuperAdmin can modify SuperAdmin roles for user {userId}");
                            continue;
                        }

                        // Prevent role changes for candidates - candidates have fixed "Candidate" role
                        if (userRoles.Contains("Candidate") && (request.RolesToAdd.Any() || request.RolesToRemove.Any()))
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Cannot modify roles for candidate accounts. Candidates have fixed 'Candidate' role.");
                            continue;
                        }

                        bool hasError = false;

                        // Add roles
                        if (request.RolesToAdd.Any())
                        {
                            var rolesToActuallyAdd = request.RolesToAdd.Except(userRoles).ToList();
                            if (rolesToActuallyAdd.Any())
                            {
                                var addResult = await _userManager.AddToRolesAsync(user, rolesToActuallyAdd);
                                if (!addResult.Succeeded)
                                {
                                    result.FailureCount++;
                                    result.Errors.Add($"Failed to add roles for user {userId}: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
                                    hasError = true;
                                }
                            }
                        }

                        // Remove roles
                        if (request.RolesToRemove.Any() && !hasError)
                        {
                            var rolesToActuallyRemove = request.RolesToRemove.Intersect(userRoles).ToList();
                            if (rolesToActuallyRemove.Any())
                            {
                                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToActuallyRemove);
                                if (!removeResult.Succeeded)
                                {
                                    result.FailureCount++;
                                    result.Errors.Add($"Failed to remove roles for user {userId}: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
                                    hasError = true;
                                }
                            }
                        }

                        if (!hasError)
                        {
                            result.SuccessCount++;
                            _logger.LogInformation(
                                "Admin {AdminId} modified roles for user {UserId}. Added: {Added}, Removed: {Removed}",
                                adminUserId,
                                userId,
                                string.Join(", ", request.RolesToAdd),
                                string.Join(", ", request.RolesToRemove)
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Error managing roles for user {userId}: {ex.Message}");
                        _logger.LogError(ex, "Error managing roles for user {UserId}", userId);
                    }
                }

                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bulk role management by admin {AdminId}", adminUserId);
                throw;
            }
        }

        public async Task<UserDetailsDto?> UpdateUserInfoAsync(Guid userId, UpdateUserInfoRequest request, Guid adminUserId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return null;

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(request.FirstName))
                    user.FirstName = request.FirstName.Trim();

                if (!string.IsNullOrWhiteSpace(request.LastName))
                    user.LastName = request.LastName.Trim();

                if (request.PhoneNumber != null)
                    user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    _logger.LogError(
                        "Failed to update user {UserId} info: {Errors}",
                        userId,
                        string.Join(", ", updateResult.Errors.Select(e => e.Description))
                    );
                    return null;
                }

                _logger.LogInformation(
                    "Admin {AdminId} updated info for user {UserId}",
                    adminUserId,
                    userId
                );

                // Return updated user details
                return await GetUserDetailsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user info for userId: {UserId}", userId);
                throw;
            }
        }

        private string GenerateTemporaryPassword(string firstName, string lastName, string? email)
        {
            // Pattern: FirstName + LastInitial + @Year

            var firstNamePart = string.IsNullOrEmpty(firstName) ? "User" : firstName;
            var lastInitial = string.IsNullOrEmpty(lastName) ? "X" : lastName.Substring(0, 1).ToUpper();
            var year = DateTime.UtcNow.Year;

            var capitalizedFirstName = char.ToUpper(firstNamePart[0]) + firstNamePart.Substring(1).ToLower();

            return $"{capitalizedFirstName}{lastInitial}@{year}";
        }
    }
}
