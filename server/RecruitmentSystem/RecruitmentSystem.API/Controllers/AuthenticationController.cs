using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;

namespace RecruitmentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("AuthPolicy")]
    public class AuthenticationController : ControllerBase
    {
        #region Dependencies & Constructor

        private readonly IAuthenticationService _authenticationService;
        private readonly IEmailService _emailService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AuthenticationController> _logger;
        private const string RefreshTokenCookieName = "refreshToken";

        public AuthenticationController(
            IAuthenticationService authenticationService,
            IEmailService emailService,
            UserManager<User> userManager,
            ILogger<AuthenticationController> logger)
        {
            _authenticationService = authenticationService;
            _emailService = emailService;
            _userManager = userManager;
            _logger = logger;
        }

        #endregion

        #region Authentication

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authenticationService.LoginAsync(loginDto, GetClientIpAddress(), GetUserAgent());
                SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiration);
                return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Login successful"));
            }
            catch (UnauthorizedAccessException ex)
            {
                // Log security event
                _logger.LogWarning("Login attempt failed for email: {Email} from IP: {IP}. Reason: {Reason}",
                    loginDto.Email, GetClientIpAddress(), ex.Message);

               return Unauthorized(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { "Invalid email or password" }, "Login failed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { "Login failed due to an unexpected error" }, "Login failed"));
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Refresh()
        {
            try
            {
                if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
                {
                    return Unauthorized(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { "Refresh token is missing." }, "Token refresh failed"));
                }

                var result = await _authenticationService.RefreshTokenAsync(refreshToken, GetClientIpAddress(), GetUserAgent());
                SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiration);

                return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Token refreshed successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                ClearRefreshTokenCookie();
                return Unauthorized(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { ex.Message }, "Token refresh failed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing authentication token");
                return StatusCode(500, ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { "Token refresh failed due to an unexpected error" }, "Token refresh failed"));
            }
        }

        #endregion

        #region Registration

        // candidate registration
        [HttpPost("register/candidate")]
        public async Task<ActionResult<RegisterResponseDto>> RegisterCandidate([FromBody] CandidateRegisterDto registerDto)
        {
            try
            {
                var result = await _authenticationService.RegisterCandidateAsync(registerDto);

                var user = await _userManager.FindByEmailAsync(registerDto.Email);
                if (user != null)
                {
                    // verification email
                    var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    var verificationUrl = Url.Action("ConfirmEmail", "Authentication",
                        new { userId = user.Id, token = emailToken }, Request.Scheme);

                    if (!string.IsNullOrEmpty(verificationUrl))
                    {
                        await _emailService.SendEmailVerificationAsync(user.Email!, user.FirstName!, emailToken, verificationUrl);
                    }
                }

                return Ok(ApiResponse<RegisterResponseDto>.SuccessResponse(result, result.Message));
            }
            catch (InvalidOperationException)
            {
                return BadRequest(ApiResponse<RegisterResponseDto>.FailureResponse(new List<string> { "Registration failed due to a validation error" }, "Registration failed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during candidate registration for email: {Email}", registerDto.Email);
                return BadRequest(ApiResponse<RegisterResponseDto>.FailureResponse(new List<string> { "Registration failed due to an unexpected error" }, "Registration failed"));
            }
        }

        // Admin-only registration for Recruiter, HR, etc.
        [HttpPost("register/staff")]
        [Authorize(Roles = "SuperAdmin, Admin, HR")]
        public async Task<ActionResult<AuthResponseDto>> RegisterStaff([FromBody] RegisterStaffDto dto)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { "Current user not found." }, "Unauthorized"));
                }
                var currentRoles = await _userManager.GetRolesAsync(currentUser);

                // Allow SuperAdmin registration only if current user is SuperAdmin
                if (dto.Roles.Contains("SuperAdmin") && !currentRoles.Contains("SuperAdmin"))
                {
                    return Forbid("Only SuperAdmins can register other SuperAdmins.");
                }

                var result = await _authenticationService.RegisterStaffAsync(dto, GetClientIpAddress(), GetUserAgent());

                // welcome email
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user != null)
                {
                    var roles = string.Join(", ", dto.Roles);
                    await _emailService.SendStaffRegistrationEmailAsync(user.Email!, user.FirstName!, roles);
                }

                SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiration);

                return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Staff registration successful"));
            }
            catch (InvalidOperationException)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { "Staff registration failed due to a validation error" }, "Staff registration failed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during staff registration for email: {Email}", dto.Email);
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { "Staff registration failed due to an unexpected error" }, "Staff registration failed"));
            }
        }

        // Initial Super Admin registration
        [HttpPost("register/initial-admin")]
        public async Task<ActionResult<AuthResponseDto>> RegisterInitialAdmin([FromBody] InitialAdminDto registerDto)
        {
            try
            {
                var hasAdmin = await _authenticationService.HasSuperAdminAsync();
                if (hasAdmin)
                {
                    return Forbid("Super Admin already exists. Use staff registration endpoint.");
                }

                var result = await _authenticationService.RegisterInitialSuperAdminAsync(registerDto, GetClientIpAddress(), GetUserAgent());

                var user = await _userManager.FindByEmailAsync(registerDto.Email);
                if (user != null)
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName!);
                }

                SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiration);

                return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Initial Super Admin registration successful"));
            }
            catch (InvalidOperationException)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { "Initial Super Admin registration failed due to a validation error" }, "Initial Super Admin registration failed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initial admin registration for email: {Email}", registerDto.Email);
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { "Initial Super Admin registration failed due to an unexpected error" }, "Initial Super Admin registration failed"));
            }
        }

        /// <summary>
        /// Bulk register candidates from Excel file
        /// </summary>
        [HttpPost("register/bulk-candidates")]
        [Authorize(Roles = "SuperAdmin,Admin,HR,Recruiter")]
        public async Task<ActionResult<List<RegisterResponseDto>>> BulkRegisterCandidates(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<List<RegisterResponseDto>>.FailureResponse(new List<string> { "Please provide a valid Excel file" }, "Invalid File"));
                }

                var results = await _authenticationService.BulkRegisterCandidatesAsync(file);
                return Ok(ApiResponse<List<RegisterResponseDto>>.SuccessResponse(results, "Bulk registration completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk candidate registration");
                return StatusCode(500, ApiResponse<List<RegisterResponseDto>>.FailureResponse(new List<string> { "An error occurred during bulk registration" }, "Bulk Registration Failed"));
            }
        }

        #endregion

        #region Password Management

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _authenticationService.ChangePasswordAsync(userId, changePasswordDto);

                if (result)
                    return Ok(ApiResponse.SuccessResponse("Password changed successfully."));
                else
                    return BadRequest(ApiResponse.FailureResponse(new List<string> { "Password change failed." }));
            }
            catch (Exception ex)
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                _logger.LogError(ex, "Error during password change for user {UserId}", userId);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "Password change failed due to an unexpected error" }, "Password change failed."));
            }
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
                if (user != null)
                {
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetUrl = Url.Action("ResetPassword", "Authentication",
                        new { userId = user.Id, token = resetToken }, Request.Scheme);

                    if (!string.IsNullOrEmpty(resetUrl))
                    {
                        await _emailService.SendPasswordResetAsync(user.Email!, user.FirstName!, resetToken, resetUrl);
                    }
                }

                return Ok(ApiResponse.SuccessResponse("If your email is registered, you will receive a password reset link."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password request for email: {Email}", forgotPasswordDto.Email);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "Failed to process forgot password request due to an unexpected error" }, "Failed to process forgot password request."));
            }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var decodedToken = WebUtility.UrlDecode(resetPasswordDto.Token);
                var user = await _userManager.FindByIdAsync(resetPasswordDto.UserId);
                if (user == null)
                {
                    return BadRequest(ApiResponse.FailureResponse(new List<string> { "Invalid request." }));
                }

                var result = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);
                if (result.Succeeded)
                {
                    return Ok(ApiResponse.SuccessResponse("Password reset successfully."));
                }
                else
                {
                    return BadRequest(ApiResponse.FailureResponse(result.Errors.Select(e => e.Description).ToList()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for user: {UserId}", resetPasswordDto.UserId);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "Password reset failed due to an unexpected error" }, "Password reset failed."));
            }
        }

        #endregion

        #region Email Verification

        [HttpPost("confirm-email")]
        public async Task<ActionResult> ConfirmEmail([FromBody] ConfirmEmailDto confirmEmailDto)
        {
            try
            {
                var decodedToken = WebUtility.UrlDecode(confirmEmailDto.Token);
                var user = await _userManager.FindByIdAsync(confirmEmailDto.UserId);

                if (user == null)
                {
                    return BadRequest(ApiResponse.FailureResponse(new List<string> { "Invalid user." }, "Email confirmation failed."));
                }

                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (result.Succeeded)
                {
                    // welcome email
                    await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName);
                    return Ok(ApiResponse.SuccessResponse("Email confirmed successfully."));
                }
                else
                {
                    return BadRequest(ApiResponse.FailureResponse(result.Errors.Select(e => e.Description).ToList(), "Email confirmation failed."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email confirmation for user: {UserId}", confirmEmailDto.UserId);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "Email confirmation failed due to an unexpected error" }, "Email confirmation failed."));
            }
        }

        [HttpPost("resend-verification")]
        public async Task<ActionResult> ResendEmailVerification([FromBody] ResendVerificationDto resendDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(resendDto.Email);
                if (user == null)
                {
                    return Ok(ApiResponse.SuccessResponse("If your email is registered, you will receive a verification link."));
                }

                if (user.EmailConfirmed)
                {
                    return BadRequest(ApiResponse.FailureResponse(new List<string> { "Email is already verified." }, "Email Already Verified"));
                }

                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var verificationUrl = Url.Action("ConfirmEmail", "Authentication",
                    new { userId = user.Id, token = emailToken }, Request.Scheme);

                if (!string.IsNullOrEmpty(verificationUrl))
                {
                    await _emailService.SendEmailVerificationAsync(user.Email!, user.FirstName!, emailToken, verificationUrl);
                }

                return Ok(ApiResponse.SuccessResponse("If your email is registered, you will receive a verification link."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification resend for email: {Email}", resendDto.Email);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "Email verification failed due to an unexpected error" }, "Email verification failed."));
            }
        }

        #endregion

        #region Profile Management

        // Profile
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var profile = await _authenticationService.GetUserProfileAsync(userId);
                return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile, "Profile retrieved successfully."));
            }
            catch (Exception ex)
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                _logger.LogError(ex, "Error during profile retrieval for user: {UserId}", userId);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "Profile retrieval failed due to an unexpected error" }, "Profile retrieval failed."));
            }
        }

        [HttpPost("logout")]
        [Authorize]

        // Logout
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken);
                await _authenticationService.LogoutAsync(userId, refreshToken, GetClientIpAddress());
                ClearRefreshTokenCookie();
                return Ok(ApiResponse.SuccessResponse("Logout successful."));
            }
            catch (Exception ex)
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "Logout failed due to an unexpected error" }, "Logout failed."));
            }
        }

        // Get user roles
        [HttpGet("roles")]
        [Authorize]
        public async Task<ActionResult<List<string>>> GetUserRoles()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var roles = await _authenticationService.GetUserRolesAsync(userId);
                return Ok(ApiResponse<List<string>>.SuccessResponse(roles, "Roles retrieved successfully."));
            }
            catch (Exception ex)
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                _logger.LogError(ex, "Error during role retrieval for user: {UserId}", userId);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "Failed to retrieve roles due to an unexpected error" }, "Failed to retrieve roles."));
            }
        }

        #endregion

        #region User Management

        [HttpDelete("delete-user/{userId}")]
        [Authorize]
        public async Task<ActionResult> DeleteUser(Guid userId)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse.FailureResponse(new List<string> { "Current user not found." }));
                }
                var targetUser = await _userManager.FindByIdAsync(userId.ToString());

                if (targetUser == null)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { "User not found." }));
                }

                var targetRoles = await _userManager.GetRolesAsync(targetUser);

                // No one can delete SuperAdmin except himself
                if (targetRoles.Contains("SuperAdmin") && currentUserId != userId)
                {
                    return Forbid("Cannot delete SuperAdmin accounts.");
                }

                // Allow deletion if current user is authorized (SuperAdmin, Admin, HR) or deleting self
                var currentRoles = await _userManager.GetRolesAsync(currentUser);
                bool isAuthorized = currentRoles.Contains("SuperAdmin") || currentRoles.Contains("Admin") || currentRoles.Contains("HR") || currentUserId == userId;

                if (!isAuthorized)
                {
                    return Forbid("You do not have permission to delete this user.");
                }

                var result = await _userManager.DeleteAsync(targetUser);
                if (result.Succeeded)
                {
                    return Ok(ApiResponse.SuccessResponse("User deleted successfully."));
                }
                else
                {
                    return BadRequest(ApiResponse.FailureResponse(result.Errors.Select(e => e.Description).ToList()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user deletion for user: {UserId}", userId);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "User deletion failed due to an unexpected error" }, "User deletion failed."));
            }
        }

        #endregion

        #region System Setup

        // To check if initial super admin setup is needed
        [HttpGet("needs-setup")]
        public async Task<ActionResult<bool>> NeedsInitialSetup()
        {
            try
            {
                var hasAdmin = await _authenticationService.HasSuperAdminAsync();
                return Ok(ApiResponse<bool>.SuccessResponse(!hasAdmin, "Setup status retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during setup status check");
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "Failed to retrieve setup status due to an unexpected error" }, "Failed to retrieve setup status."));
            }
        }

        #endregion

        #region Account Management

        [HttpPost("unlock-account/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin,HR")]
        public async Task<ActionResult> UnlockAccount(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { "User not found." }));
                }

                var result = await _userManager.SetLockoutEndDateAsync(user, null);
                if (result.Succeeded)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                    _logger.LogInformation("Account unlocked for user: {UserId} by admin: {AdminId}",
                        userId, User.FindFirstValue(ClaimTypes.NameIdentifier));
                    return Ok(ApiResponse.SuccessResponse("Account unlocked successfully."));
                }
                else
                {
                    return BadRequest(ApiResponse.FailureResponse(result.Errors.Select(e => e.Description).ToList()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking account for user: {UserId}", userId);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { "Failed to unlock account due to an unexpected error" }, "Account unlock failed."));
            }
        }

        #endregion

        #region Helpers

        private void SetRefreshTokenCookie(string? token, DateTime? expiresAt)
        {
            if (string.IsNullOrWhiteSpace(token) || !expiresAt.HasValue)
            {
                return;
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expiresAt.Value,
                Path = "/"
            };

            Response.Cookies.Append(RefreshTokenCookieName, token, cookieOptions);
        }

        private void ClearRefreshTokenCookie()
        {
            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private string? GetUserAgent()
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
        }

        #endregion
    }
}