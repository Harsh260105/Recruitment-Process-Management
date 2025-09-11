using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
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

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authenticationService.LoginAsync(loginDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // candidate registration
        [HttpPost("register/candidate")]
        public async Task<ActionResult<AuthResponseDto>> RegisterCandidate([FromBody] CandidateRegisterDto registerDto)
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
                        await _emailService.SendEmailVerificationAsync(user.Email, user.FirstName, emailToken, verificationUrl);
                    }
                }
                
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Admin-only registration for Recruiter, HR, etc.
        [HttpPost("register/staff")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<AuthResponseDto>> RegisterStaff([FromBody] RegisterDto registerDto)
        {
            try
            {
                var result = await _authenticationService.RegisterAsync(registerDto);
                
                // welcome email
                var user = await _userManager.FindByEmailAsync(registerDto.Email);
                if (user != null)
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
                }
                
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
                }
                
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _authenticationService.ChangePasswordAsync(userId, changePasswordDto);

                if (result)
                    return Ok(new { message = "Password changed successfully" });
                else
                    return BadRequest(new { message = "Password change failed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
                if (user == null)
                {
                    return Ok(new { message = "If your email is registered, you will receive a password reset link." });
                }

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetUrl = Url.Action("ResetPassword", "Authentication", 
                    new { userId = user.Id, token = resetToken }, Request.Scheme);

                if (!string.IsNullOrEmpty(resetUrl))
                {
                    await _emailService.SendPasswordResetAsync(user.Email, user.FirstName, resetToken, resetUrl);
                }

                return Ok(new { message = "If your email is registered, you will receive a password reset link." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                    return BadRequest(new { message = "Invalid request." });
                }

                var result = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);
                if (result.Succeeded)
                {
                    return Ok(new { message = "Password reset successfully." });
                }
                else
                {
                    return BadRequest(new { message = "Password reset failed.", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("confirm-email")]
        public async Task<ActionResult> ConfirmEmail([FromBody] ConfirmEmailDto confirmEmailDto)
        {
            try
            {
                var decodedToken = WebUtility.UrlDecode(confirmEmailDto.Token);
                var user = await _userManager.FindByIdAsync(confirmEmailDto.UserId);
                
                if (user == null)
                {
                    return BadRequest(new { message = "Invalid request." });
                }

                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (result.Succeeded)
                {
                    // welcome email
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
                    return Ok(new { message = "Email confirmed successfully." });
                }
                else
                {
                    return BadRequest(new { message = "Email confirmation failed.", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                    return Ok(new { message = "If your email is registered, you will receive a verification link." });
                }

                if (user.EmailConfirmed)
                {
                    return BadRequest(new { message = "Email is already verified." });
                }

                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var verificationUrl = Url.Action("ConfirmEmail", "Authentication", 
                    new { userId = user.Id, token = emailToken }, Request.Scheme);

                if (!string.IsNullOrEmpty(verificationUrl))
                {
                    await _emailService.SendEmailVerificationAsync(user.Email, user.FirstName, emailToken, verificationUrl);
                }

                return Ok(new { message = "If your email is registered, you will receive a verification link." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Profile
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var profile = await _authenticationService.GetUserProfileAsync(userId);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]

        // Logout
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _authenticationService.LogoutAsync(userId);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Get user roles
        [HttpGet("roles")]
        [Authorize]
        public async Task<ActionResult<List<string>>> GetUserRoles()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var roles = await _authenticationService.GetUserRolesAsync(userId);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // To check if initial super admin setup is needed
        [HttpGet("needs-setup")]
        public async Task<ActionResult<bool>> NeedsInitialSetup()
        {
            try
            {
                var hasAdmin = await _authenticationService.HasSuperAdminAsync();
                return Ok(!hasAdmin); 
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}