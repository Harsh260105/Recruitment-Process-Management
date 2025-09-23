using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
    public class AuthenticationController : ControllerBase
    {
        #region Dependencies & Constructor

        private readonly IAuthenticationService _authenticationService;
        private readonly IEmailService _emailService;
        private readonly UserManager<User> _userManager;

        public AuthenticationController(
            IAuthenticationService authenticationService,
            IEmailService emailService,
            UserManager<User> userManager)
        {
            _authenticationService = authenticationService;
            _emailService = emailService;
            _userManager = userManager;
        }

        #endregion

        #region Authentication

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authenticationService.LoginAsync(loginDto);
                return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Login successful"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { ex.Message }, "Login failed"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { ex.Message }, "Login failed"));
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<RegisterResponseDto>.FailureResponse(new List<string> { ex.Message }, "Registration failed"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<RegisterResponseDto>.FailureResponse(new List<string> { ex.Message }, "Registration failed"));
            }
        }

        // Admin-only registration for Recruiter, HR, etc.
        [HttpPost("register/staff")]
        [Authorize(Roles = "SuperAdmin, Admin, HR")]
        public async Task<ActionResult<AuthResponseDto>> RegisterStaff([FromBody] RegisterStaffDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

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

                var result = await _authenticationService.RegisterStaffAsync(dto);

                // welcome email
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user != null)
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName!);
                }

                return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Staff registration successful"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { ex.Message }, "Staff registration failed"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { ex.Message }, "Staff registration failed"));
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

                var result = await _authenticationService.RegisterInitialSuperAdminAsync(registerDto);

                var user = await _userManager.FindByEmailAsync(registerDto.Email);
                if (user != null)
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName!);
                }

                return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Initial Super Admin registration successful"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { ex.Message }, "Initial Super Admin registration failed"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse(new List<string> { ex.Message }, "Initial Super Admin registration failed"));
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
                return StatusCode(500, ApiResponse<List<RegisterResponseDto>>.FailureResponse(new List<string> { $"An error occurred during bulk registration: {ex.Message}" }, "Bulk Registration Failed"));
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
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Password change failed."));
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
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Failed to process forgot password request."));
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
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Password reset failed."));
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
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Email confirmation failed."));
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
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Email verification failed."));
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
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Profile retrieval failed."));
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
                await _authenticationService.LogoutAsync(userId);
                return Ok(ApiResponse.SuccessResponse("Logout successful."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Logout failed."));
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
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Failed to retrieve roles."));
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
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "User deletion failed."));
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
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Failed to retrieve setup status."));
            }
        }

        #endregion
    }
}